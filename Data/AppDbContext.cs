using Microsoft.EntityFrameworkCore;
using EchoNet.Models;

namespace EchoNet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Song> Songs => Set<Song>(); // Song Table
    public DbSet<MusicFolder> MusicFolders => Set<MusicFolder>(); // Music Folder Table
}