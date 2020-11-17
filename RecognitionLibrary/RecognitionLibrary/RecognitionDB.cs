namespace RecognitionLibrary
{
    using Microsoft.EntityFrameworkCore;
    using System.Linq;
    using System.ComponentModel.DataAnnotations;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using System.IO;

    public class RecognitionImage
    {
        public long Id { get; set; }
        public string Path { get; set; }
        public float Confidence { get; set; }
        public Blob ImageDetails { get; set; }
        public int Label { get; set; }
        [ConcurrencyCheck]
        public long Statistic { get; set; }
    }

    

    public class Blob
    {
        public long Id { get; set; }
        //public string Path { get; set; }
        public byte[] Image { get; set; }
    }

    /*public class RecognitionLabel
    {
        public long Id { get; set; } //of image
        public int Label { get; set; }
        //public virtual ICollection<RecognitionImage> Images { get; set; }

    }*/

    public class RecognitionLibraryContext : DbContext
    {
        string file = @"library.db";
        public static string curDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.FullName;
        public DbSet<RecognitionImage> Images { get; set; }
        // public DbSet<RecognitionLabel> Labels { get; set; }
        public DbSet<Blob> ImagesDetails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlite("Data Source = " + file);

        public void Clear()
        {
            foreach (var item in Images)
            {
                Images.Remove(item);
            }

            foreach (var item in ImagesDetails)
            {
                ImagesDetails.Remove(item);
            }

            SaveChanges();
        }
         public RecognitionImage FindOne(RecognitionInfo like)
        {
            if (Images != null && Images.Count() == 0)
                return null;
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
                            return image;
                        }
                    }
                }
            }
            return null;
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

        /* public override EntityEntry<RecognitionInfo> Add<RecognitionInfo>(RecognitionInfo item)
           {
               var nItem = new RecognitionImage() { Path = item.Path;  }
               Images.Add(nItem);
           }*/
    }

}
