namespace MarbleCraftOMS.Core.Entities;
public class Product {
     public int Id { get; set; } 
    public string Name { get; set; } = string.Empty;
     public string Material { get; set; } = string.Empty;
     public string Format { get; set; } = string.Empty;
     public string Surface { get; set; } = string.Empty;
     public string Color { get; set; } = string.Empty;
     public string Size { get; set; } = string.Empty;
     public string CountryOfOrigin { get; set; } = string.Empty;
     public decimal PricePerUnit { get; set; }

     // foreign key 
        public int SupplierId { get; set; }      
        public Supplier? Supplier { get; set; }

     /*
       This tells EF Core that SupplierId is not just a random number 
       — it actually links to a real Supplier object. With this, 
       when you fetch a Product you can also get its supplier details in one query.
    */

      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
     
     }