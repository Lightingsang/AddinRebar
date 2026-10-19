namespace HPDcePro.Core.XData
{
    /// <summary>
    /// Codec interface for bidirectional serialization between domain models and AutoCAD XData packets.
    /// </summary>
    /// <typeparam name="T">Target domain model type.</typeparam>
    public interface IXDataCodec<T>
    {
        string RegAppName { get; }
        XDataPacket Encode(T model);
        bool TryDecode(XDataPacket packet, out T? result);
        T Decode(XDataPacket packet);
    }
}
