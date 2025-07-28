namespace IM.Core.Interfaces;

public interface IStorageService
{
    Task<bool> UploadFile(byte[] fileBytes, string dropboxFolder, string fileName);
}