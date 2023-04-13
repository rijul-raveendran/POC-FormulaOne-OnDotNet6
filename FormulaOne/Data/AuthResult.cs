namespace FormulaOne.Data
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string Token { get; set; }
        public List<string> Errors { get; set; }
    }
}
