using UnityEngine;

namespace SceneExplorer.ToBeReplaced.Helpers
{
    public static class PaginationHelpers
    {
        public static bool ValidatePageSizeString<T>(string current, ref Pagination<T> pagination)
        {
            if (!int.TryParse(current, out int s) || (s < 5 || s > 1000)) return false;
            
            pagination.ItemPerPage = s;
            return true;

        }

        public static (int first, int last) CalculateFirstLast<T>(ref Pagination<T> pagination)
        {
            int first = pagination.ItemPerPage * (pagination.CurrentPage - 1);
            return (first, first + (pagination.ItemPerPage - 1) > pagination.ItemCount ? pagination.ItemCount : first + (pagination.ItemPerPage));
        }

        public static void RenderPageRangeError()
        {
            Color temp = GUI.color;
            GUI.color = Color.red;
            GUILayout.Label("Invalid value. Min. 5, max. 999", options: null);
            GUI.color = temp;
        }
    }
}
