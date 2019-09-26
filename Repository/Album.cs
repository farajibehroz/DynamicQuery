namespace Repository
{
    public class Album
    {
        public int Quantity { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }

        public Singer Singer { get; set; }
        public override string ToString()
        {
            return string.Format("{0} {1} {2} SingerName: {3} Ag: {4}", Quantity, Title, Artist, Singer.FirstName, Singer.Age);
        }
    }
}
