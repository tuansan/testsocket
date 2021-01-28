namespace NetSockets.Models
{
    public class SocketModel
    {
        public int Action { get; set; }
        public string Text { get; set; }
    }

    public enum ENActionSend
    {
        CHAT,
        POPUP,
        BAT_DAU,
        XAC_NHAN,
        KET_THUC
    }

    public enum ENActionReceive
    {
        CHAT,
        DAT_XE,
        HUY_DAT,
        OK,
        CANCEL,
        END
    }
}