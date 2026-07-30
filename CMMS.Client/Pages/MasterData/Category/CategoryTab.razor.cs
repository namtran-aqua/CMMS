using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using CMMS.Shared.Dtos.SpareParts;
using AntDesign;

namespace CMMS.Client.Pages.MasterData.Category
{
    public partial class CategoryTab : ComponentBase
    {
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private IMessageService Message { get; set; } = default!;

        private List<SparePartCategoryDto> Categories { get; set; } = new();
        private bool isLoading = false;

        private bool isCategoryModalVisible = false;
        private string modalTitle = "Add Category";
        private SparePartCategoryDto editingCategory = new();
        private bool isEditMode = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadCategories();
        }

        private async Task LoadCategories()
        {
            isLoading = true;
            try
            {
                var cats = await Http.GetFromJsonAsync<List<SparePartCategoryDto>>("api/SparePart/categories");
                if (cats != null)
                {
                    Categories = cats;
                }
            }
            catch (Exception ex)
            {
                Message.Error("Error loading categories: " + ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        private void ShowAddCategoryModal()
        {
            editingCategory = new SparePartCategoryDto();
            isEditMode = false;
            modalTitle = "Add Category";
            isCategoryModalVisible = true;
        }

        private void ShowEditCategoryModal(SparePartCategoryDto category)
        {
            editingCategory = new SparePartCategoryDto 
            { 
                CategoryID = category.CategoryID, 
                CategoryName = category.CategoryName 
            };
            isEditMode = true;
            modalTitle = "Edit Category";
            isCategoryModalVisible = true;
        }

        private void HandleCategoryCancel()
        {
            isCategoryModalVisible = false;
        }

        private async Task HandleCategoryOk()
        {
            if (string.IsNullOrWhiteSpace(editingCategory.CategoryName))
            {
                Message.Warning("Category name cannot be empty");
                return;
            }

            try
            {
                HttpResponseMessage response;
                if (isEditMode)
                {
                    response = await Http.PutAsJsonAsync("api/SparePart/category/update", editingCategory);
                }
                else
                {
                    response = await Http.PostAsJsonAsync("api/SparePart/category/create", editingCategory);
                }

                if (response.IsSuccessStatusCode)
                {
                    Message.Success(isEditMode ? "Category updated successfully" : "Category added successfully");
                    isCategoryModalVisible = false;
                    await LoadCategories();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Message.Error($"Failed to save category: {error}");
                }
            }
            catch (Exception ex)
            {
                Message.Error("Error saving category: " + ex.Message);
            }
        }

        private async Task DeleteCategory(int id)
        {
            try
            {
                var response = await Http.DeleteAsync($"api/SparePart/category/delete/{id}");
                if (response.IsSuccessStatusCode)
                {
                    Message.Success("Category deleted successfully");
                    await LoadCategories();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Message.Error($"Failed to delete category: {error}");
                }
            }
            catch (Exception ex)
            {
                Message.Error("Error deleting category: " + ex.Message);
            }
        }
    }
}
