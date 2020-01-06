using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BangazonAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BangazonAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {

        private readonly IConfiguration _config;

        public CustomersController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        // GET: api/Customers
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string q, [FromQuery] string include)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT 
                                        Id, 
                                        FirstName, 
                                        LastName, 
                                        Active, 
                                        CreatedDate,
                                        Address,
                                        City,
                                        State,
                                        Email,
                                        Phone
                                        FROM Customer
                                        WHERE 1=1 AND Active = 1";

                    if (include == "products")
                    {
                        cmd.CommandText = @"SELECT 
                                            c.Id,
                                            c.FirstName, 
                                            c.LastName, 
                                            c.Active, 
                                            c.CreatedDate,
                                            c.[Address],
                                            c.City,
                                            c.[State],
                                            c.Email,
                                            c.Phone,
                                            p.Id AS ProdId,
                                            p.Title,
                                            p.[Description],
                                            p.Price,
                                            p.DateAdded,
                                            p.ProductTypeId,
                                            pt.Name,
                                            pt.Id AS ProdTypeId
                                            FROM Customer c
                                            LEFT JOIN Product p ON p.CustomerId = c.Id
                                            LEFT JOIN ProductType pt ON pt.Id = p.ProductTypeId
                                            WHERE 1 = 1 AND c.Active = 1";
                    }

                    if (q != null)
                    {
                        cmd.CommandText += "AND c.FirstName LIKE @q OR c.LastName LIKE @q";
                        cmd.Parameters.Add(new SqlParameter("@q", "%" + q + "%"));
                    }

                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Customer> allCustomers = new List<Customer>();

                    while (reader.Read())
                    {
                        Customer cust = new Customer
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            Active = reader.GetBoolean(reader.GetOrdinal("Active")),
                            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                            Address = reader.GetString(reader.GetOrdinal("Address")),
                            City = reader.GetString(reader.GetOrdinal("City")),
                            State = reader.GetString(reader.GetOrdinal("State")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                            Phone = reader.GetString(reader.GetOrdinal("Phone")),
                            Products = new List<Product>()
                        };
                        if (include == "products" && ProductExists(reader, reader.GetOrdinal("ProdId")))
                        {
                            Product newProduct = new Product()
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("ProdId")),
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                Price = reader.GetSqlMoney(reader.GetOrdinal("Price")).ToDouble(),
                                DateAdded = reader.GetDateTime(reader.GetOrdinal("DateAdded")),
                                ProductTypeId = reader.GetInt32(reader.GetOrdinal("ProductTypeId")),
                                ProductType = new ProductType()
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("ProdTypeId")),
                                    Name = reader.GetString(reader.GetOrdinal("Name"))
                                }

                            };
                            if (!allCustomers.Exists(c => c.Id == cust.Id))
                            {
                                cust.Products.Add(newProduct);
                                allCustomers.Add(cust);
                            }
                            else
                            {
                                allCustomers.Find(c => c.Id == cust.Id).Products.Add(newProduct);
                            }
                        }
                        if (!allCustomers.Exists(c => c.Id == cust.Id))
                        {
                            allCustomers.Add(cust);
                        }
                    }
                    reader.Close();

                    return Ok(allCustomers);
                }
            }
        }
        private static bool ProductExists(SqlDataReader reader, int colIndex)
        {
            if (!reader.IsDBNull(colIndex))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
