using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Extension;
using DiscordBot.SemanticKernel.Core.CustomException;
using DiscordBot.SemanticKernel.Plugins.Web;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;

namespace DiscordBot.SemanticKernel.Core
{
    public class AutoFunctionInvocationFilter(KernelStatus kernelStatus, EventHandler<KernelStatus> onKenelStatusUpdatedHandler, bool showStatusPerSec = false, bool showArguments = true) : IAutoFunctionInvocationFilter, IFunctionInvocationFilter
    {
        public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
        {
            try
            {
                StepStatus stepStatus = GetStepStatus(context);
                stepStatus.Status = StatusEnum.Running;
                stepStatus.EndTime = default;
                onKenelStatusUpdatedHandler?.Invoke(this, kernelStatus);

                // Example of adding a custom approval step in the pipeline.
                //string functionKey = $"{context.Function.PluginName}-{context.Function.Name}";
                //switch (functionKey)
                //{
                //    case $"{nameof(WebPlugin)}-{nameof(WebPlugin.GoogleSearch)}":
                //    case $"{nameof(WebPlugin)}-{nameof(WebPlugin.BingAiSearch)}":
                //        stepStatus.Status = StatusEnum.AwaitingApproval;
                //        onKenelStatusUpdatedHandler?.Invoke(this, kernelStatus);

                //        Console.WriteLine($"System > The agent wants to invoke {functionKey}, do you want to proceed? (Y/N)");
                //        string shouldProceed = Console.ReadLine()!;

                //        if (!shouldProceed.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                //        {
                //            context.Result = new FunctionResult(context.Result, $"對{functionKey}的呼叫被拒絕了");

                //            stepStatus.Status = StatusEnum.Denied;
                //            stepStatus.EndTime = DateTime.Now;
                //            onKenelStatusUpdatedHandler?.Invoke(this, kernelStatus);
                //            return;
                //        }
                //        break;
                //}

                // Calling next filter in pipeline or function itself.
                // By skipping this call, next filters and function won't be invoked, and function call loop will proceed to the next function.
                await next(context);

                //stepStatus = GetStepStatus(context);
                stepStatus.Status = StatusEnum.Completed;
                stepStatus.EndTime = DateTime.Now;
                onKenelStatusUpdatedHandler?.Invoke(this, kernelStatus);
            }
            catch (ResultFailedException ex)
            {
                StepStatus stepStatus = GetStepStatus(context);
                stepStatus.Status = StatusEnum.Failed;
                stepStatus.EndTime = DateTime.Now;
                onKenelStatusUpdatedHandler?.Invoke(this, kernelStatus);
                throw;
            }
            catch (Exception ex)
            {
                StepStatus stepStatus = GetStepStatus(context);
                stepStatus.Status = StatusEnum.Error;
                stepStatus.EndTime = DateTime.Now;
                onKenelStatusUpdatedHandler?.Invoke(this, kernelStatus);
                throw;
            }
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

        private StepStatus GetStepStatus(AutoFunctionInvocationContext context)
        {
            string key = $"{context.Function.Name}";
            KernelArguments? kernelArguments = context.Arguments;
            if (!string.IsNullOrWhiteSpace(context.Function.PluginName)) key = $"{context.Function.PluginName}-{key}";

            string displayName = string.Empty;
            var runningSteps = kernelStatus.StepStatuses.Where(x => x.Status == StatusEnum.Running);
            for (int i = 0; i < runningSteps.Count(); i++) displayName = $"-{displayName}";
            if (runningSteps.Any()) displayName += " ";
            displayName += $"{key}";

            StepStatus tempStepStatus = new()
            {
                Key = key,
                KernelArguments = kernelArguments
            };
            StepStatus stepStatus = kernelStatus.StepStatuses.FirstOrDefault(x => x.GetQueueKey() == $"{tempStepStatus.GetQueueKey()}");
            if (stepStatus == null)
            {
                stepStatus = new StepStatus
                {
                    Key = key,
                    KernelArguments = kernelArguments,
                    DisplayName = displayName,
                    StartTime = DateTime.Now,
                    ShowElapsedTime = showStatusPerSec,
                    ShowArguments = showArguments,
                };
                kernelStatus.StepStatuses.Enqueue(stepStatus);
            }
            return stepStatus;
        }

        private StepStatus GetStepStatus(FunctionInvocationContext context)
        {
            string key = $"{context.Function.Name}";
            KernelArguments? kernelArguments = context.Arguments;
            if (!string.IsNullOrWhiteSpace(context.Function.PluginName)) key = $"{context.Function.PluginName}-{key}";

            string displayName = string.Empty;
            var runningSteps = kernelStatus.StepStatuses.Where(x => x.Status == StatusEnum.Running);
            for (int i = 0; i < runningSteps.Count(); i++) displayName = $"-{displayName}";
            if (runningSteps.Any()) displayName += " ";
            displayName += $"{key}";

            StepStatus stepStatus = kernelStatus.StepStatuses.FirstOrDefault(x => x.GetQueueKey() == $"{key}-{kernelArguments.ToKey()}");
            if (stepStatus == null)
            {
                stepStatus = new StepStatus
                {
                    Key = key,
                    DisplayName = displayName,
                    StartTime = DateTime.Now,
                    ShowElapsedTime = showStatusPerSec,
                    ShowArguments = showArguments,
                };
                kernelStatus.StepStatuses.Enqueue(stepStatus);
            }
            return stepStatus;
        }
    }
}
