using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReconocimientoLibros.Models
{
    public class Libro
    {
        [Key]
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public int Genero { get; set; }
        public int Anio { get; set; }
        public string Foto { get; set; }
        public string Editorial { get; set; }
    }
    public class LibreroContext : DbContext
    {
        public LibreroContext() : base("Libreria")
        {

        }
       
        public DbSet<Libro> Libro { get; set; }
       
    }
    public class LibreroInitializer : CreateDatabaseIfNotExists<LibreroContext>
    {
        protected override void Seed(LibreroContext context)
        {
            /*var estantes = new List<Estante>
            {
                new Estante{Genero="Novela Romantica"},
                new Estante{Genero="Ciencia ficcion"}
            };
            estantes.ForEach(t => context.estantes.Add(t));
            context.SaveChanges();
            var libros = new List<Libro>
            {
                new Libro{Titulo="Amor Divina Locura", Genero=1, Autor="Walter Risso", Anio=2001}
            };
            libros.ForEach(u => context.libros.Add(u));
            context.SaveChanges();
            /*var personas = new List<Persona>
            {
                new Persona{Nombre="Paola Serpa", Genero=1, Estudiante="Universitario", Correo="paola.serpa@ucuenca.edu.ec", Edad=20}
            };
            personas.ForEach(v => context.personas.Add(v));
            context.SaveChanges();*/
        }
    }

    public class LibreroConfiguration : DbConfiguration
    {
        public LibreroConfiguration()
        {
            SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy());
        }
    }
}