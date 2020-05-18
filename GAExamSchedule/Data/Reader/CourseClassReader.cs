using ExcelDataReader;
using GAExamSchedule.Algorithm;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;

namespace GAExamSchedule.Data.Reader
{
    public class CourseClassReader
    {
        #region Constants and Fields

        private string PATH_EXCEL_DATA;
        private readonly string FILENAME_COURSE_CLASS = ConfigurationManager.AppSettings.Get("data.input.filename.courseClasses");

        private const string COLUMN_NAME_ID = "ID";
        private const string COLUMN_NAME_COURSE = "Ders";
        private const string COLUMN_NAME_PROFESSOR = "Öğretim Görevlisi";
        private const string COLUMN_NAME_DURATION = "Süre";
        private const string COLUMN_NAME_GROUPS = "Öğrenci Grupları";
        private const string COLUMN_NAME_REQ_LAB = "Laboratuvar Gereksinimi (E - H)";
        private const string COLUMN_NAME_CAPACITY = "Öğrenci Sayısı";

        private const string TEXT_BOOLEAN = "E";

        private string _filePath;
        private List<CourseClass> _courseClasses = new List<CourseClass>();

        private int _idColumnNumber = 0;
        private int _courseColumnNumber = 0;
        private int _prelectorColumnNumber = 0;
        private int _durationColumnNumber = 0;
        private int _groupsColumnNumber = 0;
        private int _reqLabColumnNumber = 0;
        private int _capacityColumnNumber = 0;

        StudentGroupReader _studentGroupReader = new StudentGroupReader();
        PrelectorReader _prelectorReader = new PrelectorReader();
        CourseReader _courseReader = new CourseReader();

        #endregion

        #region Constructors

        public CourseClassReader()
        {
            PATH_EXCEL_DATA = ConfigurationManager.AppSettings.Get("data.input.location");

            if (string.IsNullOrEmpty(_filePath))
            {
                _filePath = PATH_EXCEL_DATA + (PATH_EXCEL_DATA.EndsWith("\\") ? "" : "\\") + FILENAME_COURSE_CLASS;
            }
        }

        #endregion

        #region Public Methods

        public void ResetData()
        {
            if (_courseClasses != null) _courseClasses.Clear();
        }

        public List<CourseClass> GetCourseClasses()
        {
            CollectCourseClasss();

            return _courseClasses;
        }

        public List<CourseClass> GetPrelectorCourses(int professorId)
        {
            CollectCourseClasss();

            return _courseClasses.FindAll(_c => _c.Prelector.ID.Equals(professorId));
        }

        public List<CourseClass> GetGroupsCourses(int groupId)
        {
            CollectCourseClasss();

            return _courseClasses.FindAll(_c => _c.StudentGroups.FindAll(g => g.ID.Equals(groupId)).Count > 0);
        }


        public CourseClass GetCourseClassById(int id)
        {
            CollectCourseClasss();

            return _courseClasses.Find(_r => _r.ID.Equals(id));
        }

        #endregion

        #region Private Methods

        private void CollectCourseClasss()
        {
            if (_courseClasses != null && _courseClasses.Count != 0) return;

            using (var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    do
                    {
                        while (reader.Read()) { }
                    } while (reader.NextResult());

                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        UseColumnDataType = true,
                        FilterSheet = (tableReader, sheetIndex) => true,
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true,
                            FilterRow = (rowReader) => {
                                return true;
                            },
                            FilterColumn = (rowReader, columnIndex) => {
                                return true;
                            }
                        }
                    });

                    FindColumnIndexes(result.Tables[0].Columns);

                    foreach (DataRow row in result.Tables[0].Rows)
                    {
                        List<int> _groupIds = row.ItemArray[_groupsColumnNumber].ToString().Split(',').Select(int.Parse).ToList();
                        List<StudentGroup> _studentGroups = new List<StudentGroup>();
                        _groupIds.ForEach(_id => {
                            _studentGroups.Add(_studentGroupReader.GetStudentGroupById(_id));
                        });

                        _courseClasses.Add(new CourseClass
                        {
                            ID = int.Parse(row.ItemArray[_idColumnNumber].ToString()),
                            Duration = int.Parse(row.ItemArray[_durationColumnNumber].ToString()),
                            StudentCount = int.Parse(row.ItemArray[_capacityColumnNumber].ToString()),
                            RequiresLab = row.ItemArray[_reqLabColumnNumber].ToString().ToLower().Equals(TEXT_BOOLEAN),
                            StudentGroups = _studentGroups,
                            Course = _courseReader.GetCourseById(int.Parse(row.ItemArray[_courseColumnNumber].ToString())),
                            Prelector = _prelectorReader.GetPrelectorById(int.Parse(row.ItemArray[_prelectorColumnNumber].ToString())),
                        });
                    }
                }
            }
        }

        private void FindColumnIndexes(DataColumnCollection columnCollection)
        {
            if ((_idColumnNumber == _courseColumnNumber) ||
                (_idColumnNumber == _prelectorColumnNumber) ||
                (_idColumnNumber == _durationColumnNumber) ||
                (_idColumnNumber == _groupsColumnNumber) ||
                (_idColumnNumber == _reqLabColumnNumber) ||
                (_idColumnNumber == _capacityColumnNumber) ||

                (_courseColumnNumber == _prelectorColumnNumber) ||
                (_courseColumnNumber == _durationColumnNumber) ||
                (_courseColumnNumber == _groupsColumnNumber) ||
                (_courseColumnNumber == _reqLabColumnNumber) ||
                (_courseColumnNumber == _capacityColumnNumber) ||

                (_prelectorColumnNumber == _durationColumnNumber) ||
                (_prelectorColumnNumber == _groupsColumnNumber) ||
                (_prelectorColumnNumber == _reqLabColumnNumber) ||
                (_prelectorColumnNumber == _capacityColumnNumber) ||

                (_durationColumnNumber == _groupsColumnNumber) ||
                (_durationColumnNumber == _reqLabColumnNumber) ||
                (_durationColumnNumber == _capacityColumnNumber) ||

                (_groupsColumnNumber == _reqLabColumnNumber) ||
                (_groupsColumnNumber == _capacityColumnNumber) ||

                (_reqLabColumnNumber == _capacityColumnNumber))
            {
                for (int i = 0; i < columnCollection.Count; i++)
                {
                    DataColumn column = columnCollection[i];
                    switch (column.ColumnName)
                    {
                        case COLUMN_NAME_ID:
                            _idColumnNumber = i;
                            break;
                        case COLUMN_NAME_COURSE:
                            _courseColumnNumber = i;
                            break;
                        case COLUMN_NAME_PROFESSOR:
                            _prelectorColumnNumber = i;
                            break;
                        case COLUMN_NAME_DURATION:
                            _durationColumnNumber = i;
                            break;
                        case COLUMN_NAME_GROUPS:
                            _groupsColumnNumber = i;
                            break;
                        case COLUMN_NAME_REQ_LAB:
                            _reqLabColumnNumber = i;
                            break;
                        case COLUMN_NAME_CAPACITY:
                            _capacityColumnNumber = i;
                            break;
                    }
                }
            }
        }

        #endregion

    }
}
