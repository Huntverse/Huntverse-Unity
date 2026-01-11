namespace Hunt
{
    public class WorldModel
    {
        public string worldName;
        public int congestion;
        public int myCharCount;
        
        public string GetCongestionString()
        {
            return congestion switch
            {
                1 => "원활",
                2 => "보통",
                3 => "혼잡",
                _ => "보통"
            };
        }
    }
}

