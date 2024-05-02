using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Joins;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Planning.Handlebars;

namespace DiscordBot.Helper
{
    public partial class PromptHelper
    {
        public (List<PlanStep>, string) GetStepFromPlan(HandlebarsPlan plan)
        {
            // Use reflection to access the private variable _template
            FieldInfo templateField = typeof(HandlebarsPlan).GetField("_template", BindingFlags.NonPublic | BindingFlags.Instance);
            string templateValue = (string)templateField?.GetValue(plan);
            List<PlanStep> planSteps = GetPlanStepFromString(templateValue);

            return (planSteps, templateValue);
        }

        public List<PlanStep> GetPlanStepFromString(string templateValue)
        {
            // Get all matches of the regex pattern
            List<PlanStep> planSteps = [];
            Regex stepRegex = StepRegex();

            foreach (Match match in stepRegex.Matches(templateValue).Cast<Match>())
            {
                planSteps.Add(new PlanStep
                {
                    FullDisplayName = match.Groups[2].ToString(),
                    Step = int.Parse(match.Groups[3].ToString()),
                    Description = match.Groups[4].ToString(),
                    ActionRows = match.Groups[5].Captures.Select(x => x.ToString().Replace("\n", string.Empty)).ToArray(),
                    DisplayActionRows = match.Groups[6].Captures.Select((x, index) => $"{x}{match.Groups[9].Captures[index].ToString().Replace(" --", string.Empty)}").ToArray(),
                });
            }

            return planSteps;
        }

        [GeneratedRegex(@"({{!-- (Step (\d): (.*)) --}}\s){1}((.*){{((!-- )?(.*))}}\s?)+")]
        private static partial Regex StepRegex();

        public struct PlanStep
        {
            public string FullDisplayName { get; set; }
            public int Step { get; set; }
            public string Description { get; set; }
            public string[] ActionRows { get; set; }
            public string[] DisplayActionRows { get; set; }

            public override readonly string ToString() => FullDisplayName;
        }
    }
}
