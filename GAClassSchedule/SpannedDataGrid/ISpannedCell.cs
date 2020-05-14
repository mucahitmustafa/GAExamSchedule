using System.Windows.Forms;

namespace GAClassSchedule.SpannedDataGrid
{
    interface ISpannedCell
    {
        int ColumnSpan { get; }
        int RowSpan { get; }
        DataGridViewCell OwnerCell { get; }
    }
}
