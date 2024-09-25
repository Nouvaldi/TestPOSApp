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
    public class ItemsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(AppDbContext context, ILogger<ItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseDto>> GetItems(int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }
            if (pageSize <= 0)
            {
                pageSize = 10;
            }

            var totalItems = await _context.Items.CountAsync();

            var items = await _context.Items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "success",
                Data = new
                {
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = items
                }
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Item>> GetItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Item not found"
                });
            }
            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "success",
                Data = item
            });
        }

        [HttpPost]
        public async Task<ActionResult<ResponseDto>> PostItem([FromForm] ItemDt dt)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid input data"
                });
            }

            try
            {
                var imageFileUrl = "";

                if (dt.ImageFile != null)
                {
                    var validRes = ValidateImageFile(dt.ImageFile);
                    if (!validRes.IsValid)
                    {
                        return BadRequest(new ResponseDto
                        {
                            IsSuccess = false,
                            Message = validRes.ErrorMessage,
                        });
                    }

                    var uploadRes = await UploadImageAsync(dt.ImageFile);
                    if (!uploadRes.IsSuccess)
                    {
                        return StatusCode(500, new ResponseDto
                        {
                            IsSuccess = false,
                            Message = "Failed to upload image"
                        });
                    }
                    imageFileUrl = uploadRes.ImageUrl;
                }

                var item = new Item
                {
                    Name = dt.Name,
                    Price = dt.Price,
                    Stock = dt.Stock,
                    Category = dt.Category,
                    ImageUrl = imageFileUrl,
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                var itemDto = new ItemDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Price = item.Price,
                    Stock = item.Stock,
                    Category = item.Category,
                    ImageUrl = item.ImageUrl,
                };

                return CreatedAtAction(nameof(GetItem), new { id = item.Id }, new ResponseDto
                {
                    IsSuccess = true,
                    Message = "Item created successfully",
                    Data = itemDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Failed to create new item",
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseDto>> PutItem(int id, [FromForm] ItemUpdateDt dt)
        {
            if (id != dt.Id)
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Item not found"
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid input data"
                });
            }

            var currItem = await _context.Items.FindAsync(id);
            if (currItem == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Item not found",
                });
            }

            currItem.Name = dt.Name;
            currItem.Price = dt.Price;
            currItem.Stock = dt.Stock;
            currItem.Category = dt.Category;

            if (dt.ImageFile != null)
            {
                var validRes = ValidateImageFile(dt.ImageFile);
                if (!validRes.IsValid)
                {
                    return BadRequest(new ResponseDto
                    {
                        IsSuccess = false,
                        Message = validRes.ErrorMessage
                    });
                }

                var uploadRes = await UploadImageAsync(dt.ImageFile);
                if (!uploadRes.IsSuccess)
                {
                    return StatusCode(500, new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "Failed to upload image",
                    });
                }

                if (!string.IsNullOrEmpty(currItem.ImageUrl))
                {
                    DeleteOldImage(currItem.ImageUrl);
                }

                currItem.ImageUrl = uploadRes.ImageUrl;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await ItemExistsAsync(id))
                {
                    return NotFound(new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "Item not found"
                    });
                }
                else
                {
                    _logger.LogError(ex, "Error occurred while updating item");
                    return StatusCode(500, new ResponseDto
                    {
                        IsSuccess = false,
                        Message = "Error occurred while updating item"
                    });
                }
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Item updated successfully"
            });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResponseDto>> DeleteItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Item not found"
                });
            }

            if (!string.IsNullOrEmpty(item.ImageUrl))
            {
                DeleteOldImage(item.ImageUrl);
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Item deleted successfully"
            });
        }

        [HttpGet("stock")]
        public async Task<ActionResult<ResponseDto>> GetStockReport(int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }
            if (pageSize <= 0)
            {
                pageSize = 10;
            }

            var totalItems = await _context.Items.CountAsync();

            var stockReport = await _context.Items.Select(i => new Item
            {
                Id = i.Id,
                Name = i.Name,
                Stock = i.Stock,
                Category = i.Category,
                Price = i.Price
            }).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "success",
                Data = new
                {
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    StockReport = stockReport
                }
            });
        }

        private async Task<bool> ItemExistsAsync(int id)
        {
            return await _context.Items.AnyAsync(e => e.Id == id);
        }

        private (bool IsValid, string ErrorMessage) ValidateImageFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var maxFileSizeBytes = 8 * 1024 * 1024; // 8 MB

            if (file.Length > maxFileSizeBytes)
            {
                return (false, "File size exceeds the limit of 8 MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return (false, "Only .jpg and .png files are allowed.");
            }

            return (true, "success");
        }

        private async Task<(bool IsSuccess, string ImageUrl)> UploadImageAsync(IFormFile file)
        {
            try
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var uploadPath = Path.Combine("wwwroot", "images");
                var filePath = Path.Combine(uploadPath, fileName);

                Directory.CreateDirectory(uploadPath);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return (true, "/images/" + fileName);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private void DeleteOldImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            var filePath = Path.Combine("wwwroot", imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while deleting old image");
                }
            }
        }
    }
}
