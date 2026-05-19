using MV_BL.Domain;
using MV_BL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV_Util.Registries
{
    public class TxtTestExporter : ITestExporter
    {
        public string Format => "txt";

        public void Export(Test test, string filePath, bool includeAnswers = false)
        {
            var sb = new StringBuilder();
            var ordered = test.Questions.OrderBy(q => q.QuestionOrder).ToList();

            sb.AppendLine(test.Title);
            sb.AppendLine(new string('=', test.Title.Length));
            sb.AppendLine();

            foreach (var tq in ordered)
            {
                var q = tq.Question;
                if (q is null) continue;
                sb.AppendLine($"{tq.QuestionOrder}. {q.QuestionText}");
                sb.AppendLine();
                for (int i = 0; i < tq.AnswerDisplayOrder.Count; i++)
                {
                    int origOrder = tq.AnswerDisplayOrder[i];
                    var answer = q.Answers.FirstOrDefault(a => a.OriginalOrder == origOrder);
                    if (answer is null) continue;
                    char letter = (char)('A' + i);
                    sb.AppendLine($"{letter}. {answer.AnswerText}");
                }
                sb.AppendLine();
            }

            if (includeAnswers)
            {
                sb.AppendLine("--- Answer Key ---");
                foreach (var tq in ordered)
                {
                    var q = tq.Question;
                    if (q is null) continue;
                    char correctLetter = '?';
                    for (int i = 0; i < tq.AnswerDisplayOrder.Count; i++)
                    {
                        int origOrder = tq.AnswerDisplayOrder[i];
                        var answer = q.Answers.FirstOrDefault(a => a.OriginalOrder == origOrder);
                        if (answer?.IsCorrect == true) { correctLetter = (char)('A' + i); break; }
                    }
                    sb.AppendLine($"{tq.QuestionOrder}. {correctLetter}");
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
