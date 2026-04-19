namespace TextEditor
{
    public class SemanticError
    {
        public string Message { get; }
        public string Location { get; }
        public int CharPosition { get; }

        public SemanticError(string message, string location, int charPosition = -1)
        {
            Message = message;
            Location = location;
            CharPosition = charPosition;
        }
    }
}
