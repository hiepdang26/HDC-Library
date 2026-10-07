namespace HDC.Ads.Ports
{
    internal interface IShowWithLeader
    {
        void ShowWithLeader(string leaderId);

        void CancelShowWithLeader(string leaderId);

        bool ShownWithLeader(string leaderId);
    }
}
