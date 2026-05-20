namespace ApiGeneral.AuthApi.DTOs.VenueDTOs;

public class VenueDto
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = string.Empty;
    public string Address  { get; set; } = string.Empty;
    public string City     { get; set; } = string.Empty;
    public int    Capacity { get; set; }
    public bool   IsActive { get; set; }
}