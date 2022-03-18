
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System.Reflection;


// --- configuration builder

IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", false)
            .Build();

// --- configuration and prerequisities

string cConnectionStringDownload = configuration.GetSection("DownloadSection").GetSection("ConnectionString").Value;
string cbContainerNameDownload = configuration.GetSection("DownloadSection").GetSection("ContainerName").Value;

var cbClientDownload = new BlobServiceClient(cConnectionStringDownload);
var cbContainerDownload = cbClientDownload.GetBlobContainerClient(cbContainerNameDownload);
var cClientDownload = cbClientDownload.GetBlobContainerClient(cbContainerNameDownload);


string cConnectionStringUpload = configuration.GetSection("UploadSection").GetSection("ConnectionString").Value;
string cbContainerNameUpload = configuration.GetSection("UploadSection").GetSection("ContainerName").Value;

var cbClientUpload = new BlobServiceClient(cConnectionStringUpload); ;
var cbContainerUpload = cbClientUpload.GetBlobContainerClient(cbContainerNameUpload); ;
var cClientUpload = cbClientUpload.GetBlobContainerClient(cbContainerNameUpload);


// -- multithreading semaphore

var semaphore = new SemaphoreSlim(50);
var tasks = new List<Task>();


// -- download objects & folder

var blobClientDownload = new BlobServiceClient(cConnectionStringDownload);
var containerDownload = blobClientDownload.GetBlobContainerClient(cbContainerNameDownload);

string directoryPath = Environment.CurrentDirectory + $@"\Download\{containerDownload.Name}\";

if (!Directory.Exists(directoryPath))
{
    Directory.CreateDirectory(directoryPath);
}


// download & upload itteration

await foreach (var blob in cClientDownload.GetBlobsAsync())
{
      
    string filePath = directoryPath + blob.Name;

    Console.WriteLine($"downloading {blob.Name} {blob.Properties.ContentLength / 1024} Kb, {blob.Properties.ContentType}");

    BlobClient blobClient = containerDownload.GetBlobClient(blob.Name);

    await blobClient.DownloadToAsync(filePath);

    var blocClientUpload = new BlobServiceClient(cConnectionStringUpload);
    var containerUpload = blocClientUpload.GetBlobContainerClient(cbContainerNameUpload);

    var blobHttpHeader = new BlobHttpHeaders();
    blobHttpHeader.ContentType = blob.Properties.ContentType;

    var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

    Console.WriteLine($"uploading to {containerUpload.Name} .... ");

    var uploadClient = cClientUpload.GetBlobClient(blob.Name);
    var uploadBlob = uploadClient.UploadAsync(stream, blobHttpHeader);
    
    Console.WriteLine($"");

}

await Task.WhenAll(tasks);
Console.WriteLine("---------------");
Console.WriteLine("done");
Console.ReadKey();
