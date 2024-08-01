// Copyright (c) Kevin BEAUGRAND. All rights reserved.

using DiscordBot.SemanticKernel.Plugins.KernelMemory.CodeInterpretion.Exceptions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernel.Assistants.AutoGen.Exceptions;
using SemanticKernel.Assistants.AutoGen.Plugins;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory.CodeInterpretion;

public class CodeInterpretionPlugin
{
    private readonly DockerClient _dockerClient;

    private readonly CodeInterpretionPluginOptions _options;

    private readonly ILogger<CodeInterpretionPlugin> _logger;

    private const string CodeFilePath = "/var/app/code.py";
    private const string RequirementsFilePath = "/var/app/requirements.txt";
    private const string OutputDirectoryPath = "/var/app/output";

    public CodeInterpretionPlugin(CodeInterpretionPluginOptions options, ILoggerFactory loggerFactory)
    {
        this._options = options;
        this._dockerClient = new DockerClientConfiguration(new Uri(options.DockerEndpoint)).CreateClient();

        this._logger = loggerFactory.CreateLogger<CodeInterpretionPlugin>();
    }

    [KernelFunction]
    [Description("Executes the specified python code in a sandbox.")]
    [return: Description("The result of the program execution.")]
    public async Task<string> ExecutePythonCode(
        [Description("""
        The python code to execute. 
        Make sure you follow the correct Python code syntax before submitting it.

        Do not add requirement installations here; those requirements are supposed to be in ``requirements`` input parameters.

        If you expect me to send you a result, you should use ``print`` method to output your expectactions.

        ## Example
        ```
        x = 1
        y = 2.8
        z = 1j

        print(type(x))
        print(type(y))
        print(type(z))
        ```

        This code should be sended like this: 
        ```
        x = 1\r\ny = 2.8\r\nz = 1j\r\n\r\nprint(type(x))\r\nprint(type(y))\r\nprint(type(z))
        ```
        """)] string input,
        [Description("""
        The contents of the Python requirements file to be used.
        These requirements will be added to the ``requirements.txt`` file in the sandbox by the CodeInterpreter. 

        ## Example
        ```
        requests
        tensorflow
        uvicorn
        fastapi==0.63.0CodeInterpreter.yaml
        ```
        """)] string requirements,
        [Description("""
        The list of input files to bind to the code.
        These files will be linked to the `/var/app/inputs/`` directory in the sandbox. So, when they are linked, they should be used directly in ``/var/app/inputs/``, omitting their relative path.          
        This list should be provided as a json document, and each item should be represented as a bind string.

        ## Example
        ```json
        [
              "<Full_File_Path>:/var/app/inputs/<File name>:ro",
              "/home/user/myFile.xls:/var/app/inputs/myFile.xls:rw"
        ]
        ```
        """)] string bindings,
        KernelArguments arguments = null!)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        await PullRequiredImageAsync().ConfigureAwait(false);

        var instanceId = string.Empty;

        var codeFilePath = Path.GetTempFileName();
        var requirementsFilePath = Path.GetTempFileName();
        var outputDirectory = Path.GetTempFileName();

        try
        {
            if (arguments.TryGetValue("input", out var pythonCode))
            {
                File.WriteAllText(codeFilePath, pythonCode!.ToString());
            }
            else
            {
                throw new CodeInterpreterException("The input code is not correctly provided.");
            }

            if (arguments.TryGetValue("requirements", out object? requirement))
            {
                File.WriteAllText(requirementsFilePath, requirement?.ToString());
            }

            var inputFiles = arguments.TryGetValue("bindings", out object? inputFilesValue) ? inputFilesValue!.ToString() : string.Empty;

            instanceId = await this.StartNewSandbox(@requirementsFilePath, codeFilePath, outputDirectory, inputFiles!).ConfigureAwait(false);

            this._logger.LogTrace($"Preparing Sandbox ({instanceId}:{Environment.NewLine}requirements.txt:{Environment.NewLine}{requirement}{Environment.NewLine}code.py:{Environment.NewLine}{pythonCode}");

            await this.InstallRequirementsAsync(instanceId).ConfigureAwait(false);

            var result = await this.ExecuteCodeAsync(instanceId).ConfigureAwait(false);

            this.PrepareOutputFiles(outputDirectory, arguments);

            return result!;
        }
        finally
        {
            if (!string.IsNullOrEmpty(instanceId))
            {
                await this._dockerClient.Containers.RemoveContainerAsync(instanceId, new ContainerRemoveParameters
                {
                    Force = true
                }).ConfigureAwait(false);
            }

            if (File.Exists(codeFilePath))
            {
                File.Delete(codeFilePath);
            }
            if (File.Exists(requirementsFilePath))
            {
                File.Delete(requirementsFilePath);
            }
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }
    }

    private async Task<string> StartNewSandbox(
        string requirementFilePath,
        string codeFilePath,
        string outputDirectoryPath,
        string inputFiles)
    {
        var config = new Config()
        {
            Hostname = "localhost",
        };

        if (File.Exists(outputDirectoryPath))
        {
            File.Delete(outputDirectoryPath);
        }

        if (!Directory.Exists(outputDirectoryPath))
        {
            Directory.CreateDirectory(outputDirectoryPath);
        }

        List<string>? inputBindings = [];

        if (!string.IsNullOrEmpty(inputFiles))
        {
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(inputFiles));
            inputBindings = await JsonSerializer.DeserializeAsync<List<string>>(stream).ConfigureAwait(false);
        }

        inputBindings!.AddRange(
        [
            $"{codeFilePath}:{CodeFilePath}:ro",
            $"{requirementFilePath}:{RequirementsFilePath}:ro",
            $"{outputDirectoryPath}:{OutputDirectoryPath}:rw"
        ]);


        var containerCreateOptions = new CreateContainerParameters(config)
        {
            Image = this._options.DockerImage,
            Entrypoint = ["/bin/sh"],
            Tty = true,
            NetworkDisabled = false,
            HostConfig = new HostConfig()
            {
                Binds = inputBindings
            },
            Env = [
                $"GOOGLE_SEARCH_API_KEY={this._options.GoogleSearchAPIKey}",
                $"GOOGLE_SEARCH_ENGINE_ID={this._options.GoogleSearchEngineId}"
            ]
        };

        this._logger.LogDebug("Creating container.");
        this._logger.LogTrace(JsonSerializer.Serialize(containerCreateOptions));

        var response = await _dockerClient.Containers.CreateContainerAsync(containerCreateOptions).ConfigureAwait(false);

        this._logger.LogDebug($"Starting the container (id: {response.ID}).");
        await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters()).ConfigureAwait(false);

        return response.ID;
    }

    private async Task InstallRequirementsAsync(string containerId)
    {
        _ = await this.ExecuteInContainer(containerId, $"pip install -r {RequirementsFilePath}");
    }

    private async Task<string> ExecuteCodeAsync(string containerId)
    {
        return await this.ExecuteInContainer(containerId, $"python {CodeFilePath}").ConfigureAwait(false);
    }

    private async Task<string> ExecuteInContainer(string containerId, string command)
    {
        this._logger.LogDebug($"({containerId})# {command}");

        var execContainer = await this._dockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters
        {
            AttachStderr = true,
            AttachStdout = true,
            AttachStdin = true,
            Cmd = command.Split(' ', StringSplitOptions.RemoveEmptyEntries),
            Tty = true
        }).ConfigureAwait(false);

        var multiplexedStream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(execContainer.ID, true);

        var output = await multiplexedStream.ReadOutputToEndAsync(CancellationToken.None);

        if (!string.IsNullOrWhiteSpace(output.stderr))
        {
            this._logger.LogError($"({containerId}): {output.stderr}");
            throw new CodeInterpreterException(output.stderr);
        }

        this._logger.LogDebug($"({containerId}): {output.stdout}");

        return output.stdout;
    }

    private async Task PullRequiredImageAsync()
    {
        try
        {
            _ = await _dockerClient.Images.InspectImageAsync(this._options.DockerImage);
        }
        catch (DockerImageNotFoundException)
        {
            try
            {
                await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters() { FromImage = this._options.DockerImage }, new AuthConfig(), new Progress<JSONMessage>());
            }
            catch (Exception e)
            {
                this._logger.LogWarning(e, $"Failed to create email for {this._options.DockerImage}");
            }
        }
    }

    private void PrepareOutputFiles(string outputDirectory, KernelArguments arguments)
    {
        if (arguments == null)
        {
            return;
        }

        if (!Directory.Exists(this._options.OutputFilePath))
        {
            Directory.CreateDirectory(this._options.OutputFilePath);
        }

        foreach (var item in Directory.EnumerateFiles(outputDirectory))
        {
            var fileInfo = new FileInfo(item);
            var outputFilePath = Path.Combine(this._options.OutputFilePath, fileInfo.Name);

            File.Copy(item, outputFilePath, true);
        }
    }
}
