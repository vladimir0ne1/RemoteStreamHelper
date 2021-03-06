# RemoteStreamHelper #

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=vladimir0ne1_RemoteStreamHelper&metric=alert_status)](https://sonarcloud.io/dashboard?id=vladimir0ne1_RemoteStreamHelper)
[![NuGet  Version and Downloads count](https://buildstats.info/nuget/RemoteStreamHelper)](https://www.nuget.org/packages/RemoteStreamHelper)

## A library that helps to read only required bytes from a remote storage.

Extremely helpful in situations when you need only top 10 rows from a zipped CSV file that is on a remote location such as AWS S3.

Can be combined with `ZipArchive` or `GZipStream`.
So you do not need to download full zip archive from a remote storage and then look into it.
Instead you can read only structure and see what inside without fetching full archive to your local environment.

Currently supports only AWS S3.

## Usage examples

### First init buffered AWS S3 stream reader

```c#
const string s3Path = "https://your-bucket.s3.amazonaws.com/big-data-file.zip";
var awsCredentials = new BasicAWSCredentials("<AWS_KEY>", "<AWS_SECRET>");
using var s3Client = new AmazonS3Client(awsCredentials, RegionEndpoint.USEast1);
AmazonS3Uri.TryParseAmazonS3Uri(s3Path, out var amazonS3Uri);
var s3ObjectMetadata = s3Client.GetObjectMetadataAsync(amazonS3Uri.Bucket, amazonS3Uri.Key).GetAwaiter().GetResult();

using var s3Stream = new AwsS3ReadonlyStream(s3Client, amazonS3Uri, s3ObjectMetadata);
using var cachedStream = new BufferedPagesReadonlyStream(s3Stream);
```

### Next read from GZip

```c#
using var gzipStream = new GZipStream(cachedStream, CompressionMode.Decompress);
using var reader = new StreamReader(gzipStream);
Console.WriteLine(reader.ReadLine());
Console.WriteLine(reader.ReadLine());
```

### Or read from Zip
```c#
using var zip = new ZipArchive(cachedStream);

foreach (var zipEntry in zip.Entries)
{
    Console.WriteLine(zipEntry.Name);
    var zipStream = zipEntry.Open();
    using var reader = new StreamReader(zipStream);
    Console.WriteLine(reader.ReadLine());
    Console.WriteLine(reader.ReadLine());
}
```

## License
`Remote stream helper` is Open Source software and is released under the MIT license. This license allows the use of `Remote stream helper` in free and commercial applications and libraries without restrictions.
