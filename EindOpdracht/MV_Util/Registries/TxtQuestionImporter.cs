using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MV_Util.Registries
{
    public class TxtQuestionImporter : IQuestionImporter
    {
        public string Format => "txt";

        public bool CanImport(string filePath)
            => File.Exists(filePath) && Path.GetExtension(filePath).Equals(".txt", StringComparison.OrdinalIgnoreCase);

        public IReadOnlyList<Question> Import(string filePath, int topicId)
        {
            var raw = File.ReadAllText(filePath);

            // Split off answer key (everything after "Antwoorden")
            var keyMatch = Regex.Match(raw, @"Antwoorden\s*\r?\n", RegexOptions.IgnoreCase);
            if (!keyMatch.Success)
                throw new AnswerKeyMismatchException("No 'Antwoorden' section found in source.");

            string questionsPart = raw[..keyMatch.Index];
            string keyPart = raw[(keyMatch.Index + keyMatch.Length)..];

            // Parse key: lines like "A","B (trickvraag…)","C", etc. Take leading A-E only.
            var keyLetters = new List<char>();
            foreach (var line in keyPart.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                var c = char.ToUpperInvariant(trimmed[0]);
                if (c >= 'A' && c <= 'E') keyLetters.Add(c);
            }

            // Parse questions: each starts with "N." then text; answers are "A. text" etc.
            var questions = new List<Question>();
            var qBlocks = Regex.Split(questionsPart, @"(?m)^\s*\d+\.\s+").Where(b => !string.IsNullOrWhiteSpace(b)).ToList();

            int validQuestionIndex = 0;
            for (int qi = 0; qi < qBlocks.Count; qi++)
            {
                var block = qBlocks[qi].Trim();
                var lines = block.Split('\n').Select(l => l.TrimEnd('\r').Trim()).Where(l => l.Length > 0).ToList();
                if (lines.Count == 0) continue;

                // Question text = everything until first "A. " line (also support "A) ")
                int firstAnswerIdx = lines.FindIndex(l => Regex.IsMatch(l, @"^[A-E][\.\)]\s+"));
                if (firstAnswerIdx <= 0) continue;

                string questionText = string.Join(" ", lines.Take(firstAnswerIdx)).Trim();
                var answerLines = lines.Skip(firstAnswerIdx).Where(l => Regex.IsMatch(l, @"^[A-E][\.\)]\s+")).ToList();

                if (validQuestionIndex >= keyLetters.Count)
                    throw new AnswerKeyMismatchException($"Question {validQuestionIndex + 1} has no matching answer key entry.");
                char correctLetter = keyLetters[validQuestionIndex];

                var question = new Question
                {
                    TopicId = topicId,
                    QuestionText = questionText,
                    DifficultyLevel = 1, // overridden by ImportService
                    IsFlagged = false,
                    IsActive = true
                };

                for (int ai = 0; ai < answerLines.Count; ai++)
                {
                    var match = Regex.Match(answerLines[ai], @"^([A-E])[\.\)]\s+(.*)$");
                    if (!match.Success) continue;
                    char letter = match.Groups[1].Value[0];
                    string text = match.Groups[2].Value.Trim();
                    question.Answers.Add(new Answer
                    {
                        AnswerText = text,
                        IsCorrect = letter == correctLetter,
                        OriginalOrder = ai
                    });
                }

                if (question.Answers.Count(a => a.IsCorrect) != 1)
                    throw new AnswerKeyMismatchException($"Question {qi + 1} '{questionText}': cannot resolve correct answer (key='{correctLetter}').");

                questions.Add(question);

                validQuestionIndex++;
            }

            return questions;
        }
    }
}
