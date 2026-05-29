using MV_BL.Domain;
using MV_BL.Interfaces;
using System.Text;

namespace MV_Util.Implementations;


public class TxtTestExporter : ITestExporter
{
    public string Format => "txt";

    public void Export(Test test, string filePath, bool includeAnswers = false)
    {
        var sb = new StringBuilder();

        sb.AppendLine(test.Title);
        sb.AppendLine(new string('=', test.Title.Length));
        sb.AppendLine();

        var ordered = test.Questions.OrderBy(tq => tq.SortOrder).ToList();
        foreach (var tq in ordered)
        {
            if (tq.Question is null) continue;

            // question number ,text.
            sb.AppendLine($"{tq.SortOrder}. {tq.Question.QuestionText}");
            sb.AppendLine();

            var shuffled = tq.GetShuffledAnswers();
            for (int i = 0; i < shuffled.Count; i++)
                sb.AppendLine($"{(char)('A' + i)}. {shuffled[i].AnswerText}");
            sb.AppendLine();
        }

        if (includeAnswers)
        {
            sb.AppendLine("--- Antwoorden ---");
            foreach (var tq in ordered)
            {
                if (tq.Question is null) continue;

                var correct = tq.Question.Answers.FirstOrDefault(a => a.IsCorrect);
                string letter = correct is null ? "?" : tq.AnswerIdToLetter(correct.Id);
                sb.AppendLine($"{tq.SortOrder}. {letter}");
            }
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }
}
