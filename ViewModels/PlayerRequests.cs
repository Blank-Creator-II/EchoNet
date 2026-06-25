namespace EchoNet.ViewModels;

public class SeekRequest
{
    public double Position { get; set; }
}

public class VolumeRequest
{
    public int Volume { get; set; }
}

public class SongPageStateRequest
{
    public required string viewType { get; set; }
    public required string sortType { get; set; }
}

public class RemotePlayRequest
{
    public required SongDTO songDTO { get; set; }
    public required List<SongDTO> songDTOs { get; set; }
}