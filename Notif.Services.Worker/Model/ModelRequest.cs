using FileHelpers;


namespace Notif.Services.Worker.Model
{
    [FixedLengthRecord(FixedMode.AllowMoreChars)]
    public class ModelRequest
    {
        [FieldOrder(1)]
        [FieldFixedLength(50)]
        public string appNo;

        [FieldOrder(2)]
        [FieldFixedLength(4)]
        public string appStatus;

        [FieldOrder(3)]
        [FieldFixedLength(20)]
        public string limit;
    }
}
