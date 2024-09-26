using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.SemanticKernel.Core.ResponseFormat
{
#pragma warning disable CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮新增 'required' 修飾元，或將欄位宣告為可以為 Null。
    public class MathReasoning
    {
        public List<MathReasoningStep> Steps { get; set; }

        public string FinalAnswer { get; set; }
    }

    public class MathReasoningStep
    {
        public string Explanation { get; set; }

        public string Output { get; set; }
    }
#pragma warning restore CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮新增 'required' 修飾元，或將欄位宣告為可以為 Null。
}
