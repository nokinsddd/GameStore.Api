using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.DTOs;

public record PaginationRequest(
    [property: Range(1, 10000, ErrorMessage = "Номер страницы должен быть от 1 до 10000")] 
    int PageNumber = 1,
    
    [property: Range(1, 100, ErrorMessage = "Размер страницы должен быть от 1 до 100")] 
    int PageSize = 10
);