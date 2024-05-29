using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;

namespace DiscordBot.SemanticKernel.Core
{
    public class AutoFunctionInvocationFilter(KernelStatus kernelStatus, EventHandler<KernelStatus> onKenelStatusUpdatedHandler, bool showStatusPerSec = false) : IAutoFunctionInvocationFilter, IFunctionInvocationFilter
    {
        public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
        {
            // Example: get function information
            var functionName = context.Function.Name;

            // Example: get chat history
            var chatHistory = context.ChatHistory;

            // Example: get information about all functions which will be invoked
            var functionCalls = FunctionCallContent.GetFunctionCalls(context.ChatHistory.Last());

            // Example: get request sequence index
            //this._output.WriteLine($"Request sequence index: {context.RequestSequenceIndex}");

            // Example: get function sequence index
            //this._output.WriteLine($"Function sequence index: {context.FunctionSequenceIndex}");

            // Example: get total number of functions which will be called
            //this._output.WriteLine($"Total number of functions: {context.FunctionCount}");

            // Calling next filter in pipeline or function itself.
            // By skipping this call, next filters and function won't be invoked, and function call loop will proceed to the next function.
            await next(context);

            // Example: get function result
            var result = context.Result;

            // Example: override function result value
            //context.Result = new FunctionResult(context.Result, "Result from auto function invocation filter");

            // Example: Terminate function invocation
            //context.Terminate = true;
        }

        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            StepStatus stepStatus = GetStepStatus(context);
            stepStatus.Status = StatusEnum.Running;
            stepStatus.EndTime = default;
            onKenelStatusUpdatedHandler?.Invoke(this, kernelStatus);

            await next(context);    // Call next filter in pipeline or function itself.

            stepStatus = GetStepStatus(context);
            stepStatus.Status = StatusEnum.Completed;
            stepStatus.EndTime = DateTime.Now;
            onKenelStatusUpdatedHandler?.Invoke(this, kernelStatus);
        }


        private StepStatus GetStepStatus(FunctionInvocationContext context)
        {
            string stepName = $"{context.Function.Name}";
            if (!string.IsNullOrWhiteSpace(context.Function.PluginName)) stepName = $"{context.Function.PluginName}-{stepName}";

            StepStatus stepStatus = kernelStatus.StepStatuses.FirstOrDefault(x => x.Name == stepName);
            if (stepStatus == null)
            {
                stepStatus = new StepStatus
                {
                    Name = stepName,
                    StartTime = DateTime.Now,
                    ShowElapsedTime = showStatusPerSec
                };
                kernelStatus.StepStatuses.Enqueue(stepStatus);
            }
            return stepStatus;
        }
    }
}
