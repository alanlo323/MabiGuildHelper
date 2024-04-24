using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Prompts;

namespace DiscordBot.SemanticKernel.Plugin.KernelMemory.Core
{
    public class CustomPromptProvider : IPromptProvider
    {
        private const string VerificationPrompt = """
                                              <s>
                                              You are a helpful AI assistant built by MediaTek Research. The user you are helping speaks Traditional Chinese and comes from Taiwan.
                                              [INST]
                                              Facts:
                                              {{$facts}}
                                              ======
                                              Given only the facts above, provide a comprehensive/detailed answer.
                                              You don't know where the knowledge comes from, just answer.
                                              If you don't have sufficient information, reply with '{{$notFound}}'.
                                              Question: {{$input}}
                                              Answer: 
                                              [/INST]
                                              """;
        private const string VerificationPromptChi = """
                                              <system>
                                              你是一個Discord Bot, 名字叫夏夜小幫手, 你在"夏夜月涼"伺服器裡為會員們服務.
                                              資料的內容是關於遊戲《瑪奇mabinogi》.
                                              你正在幫助的用戶來自台灣, 會講繁體中文.
                                              </system>
                                              <user>
                                              {{prompt}}
                                              </user>
                                              """;

        private readonly EmbeddedPromptProvider _fallbackProvider = new();

        public string ReadPrompt(string promptName)
        {
            switch (promptName)
            {
                case Constants.PromptNamesAnswerWithFacts:
                    //return _fallbackProvider.ReadPrompt(promptName);
                    string  basePrompt = _fallbackProvider.ReadPrompt(promptName);
                    string factPrompt = VerificationPromptChi.Replace("{{prompt}}", basePrompt);
                    return factPrompt;

                default:
                    // Fall back to the default
                    return _fallbackProvider.ReadPrompt(promptName);
            }
        }
    }
}
