using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

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
            using (var document = PdfDocument.Open(filePath))
            {
                foreach (Page page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }
            return sb.ToString();
        }
    }
}
