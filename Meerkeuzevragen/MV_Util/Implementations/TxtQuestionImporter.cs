using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using System.Text.RegularExpressions;

namespace MV_Util.Implementations;

public class TxtQuestionImporter : IQuestionImporter
{
    public string Format => "txt";

    public IReadOnlyList<Question> Import(string filePath)
    {
        var raw = File.ReadAllText(filePath);

        var keyMatch = Regex.Match(raw, @"Antwoorden\s*\r?\n", RegexOptions.IgnoreCase);
        if (!keyMatch.Success)
            throw new ImportException("No 'Antwoorden' section found in source.");

        string questionsPart = raw[..keyMatch.Index];
        string keyPart = raw[(keyMatch.Index + keyMatch.Length)..];

        var keyLetters = new List<char>();
        foreach (var line in keyPart.Split('\n'))
        {
            var t = line.Trim();
            if (t.Length == 0) continue;
            char c = char.ToUpperInvariant(t[0]);
            if (c >= 'A' && c <= 'E') keyLetters.Add(c);
        }

        var questions = new List<Question>();

        var qBlocks = Regex.Split(questionsPart, @"(?m)^\s*\d+\.\s+")
            .Where(b => !string.IsNullOrWhiteSpace(b)).ToList();

        int validIdx = 0;
        foreach (var block in qBlocks)
        {
            var lines = block.Split('\n').Select(l => l.TrimEnd('\r').Trim())
                .Where(l => l.Length > 0).ToList();
            if (lines.Count == 0) continue;

            int firstAnswerIdx = lines.FindIndex(l => Regex.IsMatch(l, @"^[A-E][\.\)]\s+"));
            if (firstAnswerIdx <= 0) continue;

            string qText = string.Join(" ", lines.Take(firstAnswerIdx)).Trim();

            var answerLines = lines.Skip(firstAnswerIdx)
                .Where(l => Regex.IsMatch(l, @"^[A-E][\.\)]\s+")).ToList();

            if (validIdx >= keyLetters.Count)
                throw new ImportException($"Question {validIdx + 1} has no matching answer-key entry.");
            char correctLetter = keyLetters[validIdx];

            var question = new Question(qText, 1);

            var answerList = new List<Answer>();
            foreach (var aLine in answerLines)
            {
                var m = Regex.Match(aLine, @"^([A-E])[\.\)]\s+(.*)$");
                if (!m.Success) continue;
                char letter = m.Groups[1].Value[0];
                string text = m.Groups[2].Value.Trim();
                answerList.Add(new Answer(text, letter == correctLetter));
            }
            question.Answers = answerList;

            questions.Add(question);
            validIdx++;
        }

        return questions;
    }
}
