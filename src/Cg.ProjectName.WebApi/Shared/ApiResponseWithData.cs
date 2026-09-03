namespace Cg.ProjectName.WebApi.Shared;

public class ApiResponseWithData<T> : ApiResponse
{
    public T? Data { get; set; }
}
