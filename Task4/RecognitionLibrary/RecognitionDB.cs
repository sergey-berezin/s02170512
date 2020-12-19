namespace RecognitionLibrary
{
    using Microsoft.EntityFrameworkCore;
    using System.Linq;
    using System.ComponentModel.DataAnnotations;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using System.IO;
    using System;
    using System.Text;

    public class RecognitionImage
    {
        public long Id { get; set; }
        public string Path { get; set; }
        public float Confidence { get; set; }
        public Blob ImageDetails { get; set; }
        public int Label { get; set; }
        public long Statistic { get; set; }
    }

    public class Blob
    {
        public long Id { get; set; }
        public byte[] Image { get; set; }
    }

    public class RecognitionLibraryContext : DbContext
    {
        string file = @"library.db";
        public static string curDir = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
        
        public DbSet<RecognitionImage> RecognitionImages { get; set; }
        public DbSet<Blob> Blobs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o) 
                                        => o.UseSqlite("Data Source = "  + Path.Combine(curDir, file));

        public void Clear()
        {
            foreach (var item in RecognitionImages)
            {
                RecognitionImages.Remove(item);
            }

            foreach (var item in Blobs)
            {
                Blobs.Remove(item);
            }

            SaveChanges();
        }

        public RecognitionImage FindOne(RecognitionInfo like)
        {
            if (RecognitionImages == null || RecognitionImages.Count() == 0)
                return null;

            using (SHA256 sha256hash = SHA256.Create())
            {
                
                var hash = GetHash(sha256hash, like.Image);
                foreach (var image in RecognitionImages.Include(i => i.ImageDetails))
                {
                    if (VerifyHash(GetHash(sha256hash, image.ImageDetails.Image), hash))
                    {
                            //image.Statistic++;
                            return image;
                    }
                }
            }
            return null;
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, byte[] img)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(img);

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        bool VerifyHash(string inhash, string hash)
        {
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            
            return comparer.Compare(inhash, hash) == 0;

        }
        
                
        /*        public List<RecognitionImage> FindAll(RecognitionInfo like)
                {
                    List<RecognitionImage> res = new List<RecognitionImage>();
                    using (SHA256 sha256hash = SHA256.Create())
                    {
                        var hash = sha256hash.ComputeHash(like.Image);
                        foreach (var image in Images)
                        {
                            var relImages = ImagesDetails.Where(i => i.Id == image.Id && image.Path == like.Path);
                            foreach (var qImage in relImages)
                            {
                                if (sha256hash.ComputeHash(qImage.Image).Equals(hash))
                                {
                                    res.Add(image);
                                }
                            }
                        }
                    }
                    return res;
                }*/

    }

}
