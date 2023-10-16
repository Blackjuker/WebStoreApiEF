namespace WebStoreApiEF.Services
{
    public class OrderHelper
    {
        /*
         * Receives a string of product identifiers, separated by '-'
         * Example : 9-9-7-9-6
         * 
         * Returns a list of pairs (dictionary):
         *      - the pair name is the product ID
         *      - the pair value is the product quantity
         * Example
         *    9:3,
         *    7:1,
         *    6:1 
         * 
         */
        public static Dictionary<int,int> GetProductDictionary(string productIdentifiers)
        {
            var  productDictionary  = new Dictionary<int,int>();

            if(productIdentifiers.Length>0)
            {
                string[] productIdArray = productIdentifiers.Split('-');
                foreach(var productId in productIdArray)
                {
                    try
                    {
                        int id =int.Parse(productId);

                        if(productDictionary.ContainsKey(id))
                        {
                            productDictionary[id] += 1;
                        }
                        else
                        {
                            productDictionary.Add(id, 1);
                        }
                    }catch(Exception ex)
                    {

                    }
                }
            }

            return productDictionary;
        } 
    }
}
