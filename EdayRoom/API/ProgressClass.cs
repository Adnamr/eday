namespace EdayRoom.API
{
    public class ProgressClass
    {
        private int _currentCount;
        public bool IsActive { get; set; }

        public int CurrentCount
        {
            get { return _currentCount; }
            set
            {
                _currentCount = value;
                if (Total != 0)
                {
                    Advance = (CurrentCount*100.0)/Total;
                }
            }
        }

        public double Advance { get; set; }
        public int Total { get; set; }
    }
}