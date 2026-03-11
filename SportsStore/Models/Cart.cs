namespace SportsStore.Models {

    public class CartLine {
        public Product Product { get; set; } = new();
        public int Quantity { get; set; }
    }

    public class Cart {
        public List<CartLine> Lines { get; set; } = new();

        public void AddItem(Product product, int quantity) {
            CartLine? line = Lines.FirstOrDefault(l => l.Product.ProductID == product.ProductID);
            if (line == null) {
                Lines.Add(new CartLine { Product = product, Quantity = quantity });
            } else {
                line.Quantity += quantity;
            }
        }

        public void RemoveLine(Product product) =>
            Lines.RemoveAll(l => l.Product.ProductID == product.ProductID);

        public void UpdateQuantity(Product product, int quantity) {
            var line = Lines.FirstOrDefault(l => l.Product.ProductID == product.ProductID);
            if (line != null) {
                if (quantity <= 0) {
                    RemoveLine(product);
                } else {
                    line.Quantity = quantity;
                }
            }
        }

        public decimal ComputeTotalValue() =>
            Lines.Sum(l => l.Product.Price * l.Quantity);

        public void Clear() => Lines.Clear();
    }
}
