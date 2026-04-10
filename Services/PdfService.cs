using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace LocalAIAgent.Services
{
    public class PdfService
    {
        private readonly AiService _ai;

        public PdfService(AiService ai)
        {
            _ai = ai;
        }

        public async Task<string> SummarizePdfAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("PDF not found.", filePath);

            var text = ExtractText(filePath);
            if (string.IsNullOrWhiteSpace(text))
                return "No readable text found in PDF.";

            return await _ai.SummarizePdfContentAsync(text);
        }

        private string ExtractText(string filePath)
        {
            var sb = new StringBuilder();

            using var reader = new PdfReader(filePath);
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                var pageText = PdfTextExtractor.GetTextFromPage(reader, i);
                sb.AppendLine(pageText);
            }

            return sb.ToString();
        }
    }
}
