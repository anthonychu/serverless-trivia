namespace ServerlessTrivia
{
    public class JServiceResponse
    {
        public long Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public JServiceResponseCategory Category { get; set; }

        public class JServiceResponseCategory
        {
            public string Title { get; set; }
        }

        public Clue ToClue()
        {
            return new Clue
            {
                Id = Id,
                Question = Question,
                Answer = Answer,
                CategoryTitle = Category.Title
            };
        }
    }
}