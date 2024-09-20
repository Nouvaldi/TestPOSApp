using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestPOSApp.Data;
using TestPOSApp.Models;

namespace TestPOSApp.Controllers
{
    [EnableCors("AllowCors")]
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class POSController : ControllerBase
    {
        private readonly AppDbContext _context;

        public POSController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("transactions")]
        public async Task<ActionResult<ResponseDto>> PostTransaction([FromBody] TransactionDt dt)
        {
            using var tran = await _context.Database.BeginTransactionAsync();

            try
            {
                decimal totalPrice = 0;
                var updatedItems = new List<(Item item, int Quantity)>();

                foreach (var item in dt.Items)
                {
                    var dbItem = await _context.Items.FindAsync(item.ItemId);
                    if (dbItem == null)
                    {
                        return NotFound(new ResponseDto { IsSuccess = false, Message = "Item not found" });
                    }

                    if (dbItem.Stock < item.Quantity)
                    {
                        return BadRequest(new ResponseDto { IsSuccess = false, Message = $"Insufficient stock for item {dbItem.Name}" });
                    }

                    totalPrice += dbItem.Price * item.Quantity;
                    updatedItems.Add((dbItem, item.Quantity));
                }

                var newTran = new Transaction
                {
                    Date = DateTime.Now,
                    TotalPrice = totalPrice,
                    Items = dt.Items.Select(i => new TransactionItem
                    {
                        ItemId = i.ItemId,
                        Quantity = i.Quantity,
                        Price = _context.Items.Find(i.ItemId).Price
                    }).ToList()
                };

                _context.Transactions.Add(newTran);

                foreach (var (item, quantity) in updatedItems)
                {
                    item.Stock -= quantity;
                    _context.Entry(item).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                await tran.CommitAsync();

                var tranDto = new TransactionDto
                {
                    Id = newTran.Id,
                    Date = newTran.Date,
                    TotalPrice = newTran.TotalPrice,
                    Items = newTran.Items.Select(i => new TransactionItemDto
                    {
                        ItemId = i.ItemId,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList(),
                };

                return CreatedAtAction(nameof(GetTransaction), new { id = newTran.Id }, new ResponseDto
                {
                    IsSuccess = true, 
                    Message = "Transaction created successfully",
                    Data = tranDto
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return StatusCode(500, new ResponseDto
                {
                    IsSuccess = false,
                    Message = "An error occurred while creating transaction. Please try again later."
                });
            }
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<ResponseDto>> GetTransactions()
        {
            var transactions = await _context.Transactions
                .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
                .Select(t => new TransactionReportDt
                {
                    TransactionId = t.Id,
                    Date = t.Date,
                    TotalPrice = t.TotalPrice,
                    Items = t.Items.Select(ti => new TransactionItemReportDt
                    {
                        ItemName = ti.Item.Name,
                        Category = ti.Item.Category,
                        Quantity = ti.Quantity,
                        Price = ti.Price
                    }).ToList()
                }).ToListAsync();

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "success",
                Data = transactions
            });
        }

        [HttpGet("transactions/{id}")]
        public async Task<ActionResult<ResponseDto>> GetTransaction(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
                .Where(t => t.Id == id)
                .Select(t => new TransactionReportDt
                {
                    TransactionId = t.Id,
                    Date = t.Date,
                    TotalPrice = t.TotalPrice,
                    Items = t.Items.Select(ti => new TransactionItemReportDt
                    {
                        ItemName = ti.Item.Name,
                        Category = ti.Item.Category,
                        Quantity = ti.Quantity,
                        Price = ti.Price
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (transaction == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Transaction not found"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "success",
                Data = transaction
            });
        }

        [HttpGet("reports")]
        public async Task<ActionResult<ResponseDto>> GetPOSReport()
        {
            var report = await _context.Transactions
                .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
                .Select(t => new TransactionReportDt
                {
                    TransactionId = t.Id,
                    Date = t.Date,
                    TotalPrice = t.TotalPrice,
                    Items = t.Items.Select(ti => new TransactionItemReportDt
                    {
                        ItemName = ti.Item.Name,
                        Category = ti.Item.Category,
                        Quantity = ti.Quantity,
                        Price = ti.Price
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "success",
                Data = report
            });
        }
    }
}
