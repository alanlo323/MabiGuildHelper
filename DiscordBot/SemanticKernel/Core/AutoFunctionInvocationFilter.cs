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
            try
            {
                StepStatus stepStatus = GetStepStatus(context);
                stepStatus.Status = StatusEnum.Running;
                stepStatus.EndTime = default;
                onKenelStatusUpdatedHandler?.Invoke(this, kernelStatus);

                // Calling next filter in pipeline or function itself.
                // By skipping this call, next filters and function won't be invoked, and function call loop will proceed to the next function.
                await next(context);

                stepStatus = GetStepStatus(context);
                stepStatus.Status = StatusEnum.Completed;
                stepStatus.EndTime = DateTime.Now;
                onKenelStatusUpdatedHandler?.Invoke(this, kernelStatus);
            }
            catch (Exception ex)
            {
                StepStatus stepStatus = GetStepStatus(context);
                stepStatus.Status = StatusEnum.Failed;
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
            if (!string.IsNullOrWhiteSpace(context.Function.PluginName)) key = $"{context.Function.PluginName}-{key}";

            string displayName = string.Empty;
            var runningSteps = kernelStatus.StepStatuses.Where(x => x.Status == StatusEnum.Running);
            for (int i = 0; i < runningSteps.Count(); i++) displayName = $"-{displayName}";
            if (runningSteps.Any()) displayName += " ";
            displayName += $"{key}";

            StepStatus stepStatus = kernelStatus.StepStatuses.FirstOrDefault(x => x.Key == key);
            if (stepStatus == null)
            {
                stepStatus = new StepStatus
                {
                    Key = key,
                    DisplayName = displayName,
                    StartTime = DateTime.Now,
                    ShowElapsedTime = showStatusPerSec
                };
                kernelStatus.StepStatuses.Enqueue(stepStatus);
            }
            return stepStatus;
        }

        private StepStatus GetStepStatus(FunctionInvocationContext context)
        {
            string key = $"{context.Function.Name}";
            if (!string.IsNullOrWhiteSpace(context.Function.PluginName)) key = $"{context.Function.PluginName}-{key}";

            string displayName = string.Empty;
            var runningSteps = kernelStatus.StepStatuses.Where(x => x.Status == StatusEnum.Running);
            for (int i = 0; i < runningSteps.Count(); i++) displayName = $"-{displayName}";
            if (runningSteps.Any()) displayName += " ";
            displayName += $"{key}";

            StepStatus stepStatus = kernelStatus.StepStatuses.FirstOrDefault(x => x.Key == key);
            if (stepStatus == null)
            {
                stepStatus = new StepStatus
                {
                    Key = key,
                    DisplayName = displayName,
                    StartTime = DateTime.Now,
                    ShowElapsedTime = showStatusPerSec
                };
                kernelStatus.StepStatuses.Enqueue(stepStatus);
            }
            return stepStatus;
        }
    }
}
