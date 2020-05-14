﻿using GAClassSchedule.Algorithm;
using GAClassSchedule.Data.Reader;
using GemBox.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;

namespace GAClassSchedule.Data.Writer
{
    public class ExcelDataWriter
    {
        #region Constants and Fields

        private string PATH_EXCEL_OUTPUT_DATA;
        private string FILENAME_OUTPUT = ConfigurationManager.AppSettings.Get("data.output.filename");

        private const string SHEET_NAME_ROOMS = "Sınıflar";
        private const string SHEET_NAME_GROUPS = "Öğrenciler";
        private const string SHEET_NAME_PRELECTORS = "Öğretim Görevlileri";

        private const int TABLE_ROW_SPACING = 2;

        object[,] HEADER_TEXTS = new object[1, 5] {
            {
                "PAZARTESİ",
                "SALI",
                "ÇARŞAMBA",
                "PERŞEMBE",
                "CUMA",
            }
        };

        object[,] TIME_SPANS = new object[1, 9] {
            {
                "9 - 10",
                "10 - 11",
                "11 - 12",
                "12 - 13",
                "13 - 14",
                "14 - 15",
                "15 - 16",
                "16 - 17",
                "17 - 18"
            }
        };

        int _excelFileCount = 1;

        PrelectorReader _prelectorReader = new PrelectorReader();
        RoomReader _roomReader = new RoomReader();
        StudentGroupReader _studentGroupReader = new StudentGroupReader();

        ExcelFile _ef;
        CellStyle _tableTitleStyle = new CellStyle();
        CellStyle _headerStyle = new CellStyle();
        CellStyle _anyCellStyle = new CellStyle();

        #endregion

        #region Constructors

        public ExcelDataWriter()
        {
            PATH_EXCEL_OUTPUT_DATA = ConfigurationManager.AppSettings.Get("data.output.location");

            SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");

            #region Group Name Style's

            _tableTitleStyle = new CellStyle();
            _tableTitleStyle.HorizontalAlignment = HorizontalAlignmentStyle.Center;
            _tableTitleStyle.VerticalAlignment = VerticalAlignmentStyle.Center;
            _tableTitleStyle.Font.Weight = ExcelFont.BoldWeight;
            _tableTitleStyle.FillPattern.SetSolid(Color.Azure);
            _tableTitleStyle.Font.Color = Color.RoyalBlue;
            _tableTitleStyle.WrapText = true;
            _tableTitleStyle.Borders.SetBorders(MultipleBorders.Right | MultipleBorders.Top, Color.Black, LineStyle.Thin);

            #endregion

            #region Header Style's
            
            _headerStyle = new CellStyle();
            _headerStyle.HorizontalAlignment = HorizontalAlignmentStyle.Center;
            _headerStyle.VerticalAlignment = VerticalAlignmentStyle.Center;
            _headerStyle.FillPattern.SetSolid(Color.DarkGray);
            _headerStyle.Font.Weight = ExcelFont.BoldWeight;
            _headerStyle.Font.Color = Color.White;
            _headerStyle.WrapText = false;
            _headerStyle.Borders.SetBorders(MultipleBorders.Outside, Color.Black, LineStyle.Thin);

            #endregion

            #region Any Cells Style's

            _anyCellStyle = new CellStyle();
            _anyCellStyle.HorizontalAlignment = HorizontalAlignmentStyle.Center;
            _anyCellStyle.VerticalAlignment = VerticalAlignmentStyle.Center;
            _anyCellStyle.Font.Size = 14 * 16;
            _anyCellStyle.Font.Weight = ExcelFont.BoldWeight;
            _anyCellStyle.FillPattern.SetSolid(Color.White);
            _anyCellStyle.Font.Color = Color.Black;
            _anyCellStyle.WrapText = true;
            _anyCellStyle.Borders.SetBorders(MultipleBorders.Outside, Color.Black, LineStyle.Thin);

            #endregion
        }

        #endregion

        #region Public Methods

        public void CreateExcelTables(Schedule schedule, List<KeyValuePair<CourseClass, int>> courseClasses)
        {
            _ef = new ExcelFile();

            SaveRoomsSchedule(schedule, courseClasses);
            SaveGroupsSchedule(schedule, courseClasses);
            SavePrelectorsSchedule(schedule, courseClasses);

            if (!PATH_EXCEL_OUTPUT_DATA.EndsWith("/")) PATH_EXCEL_OUTPUT_DATA = PATH_EXCEL_OUTPUT_DATA + "/";
            _ef.Save($"{PATH_EXCEL_OUTPUT_DATA}{FILENAME_OUTPUT}");
        }

        #endregion

        #region Private Methods

        private void SaveRoomsSchedule(Schedule schedule, List<KeyValuePair<CourseClass, int>> courseClasses)
        {
            List<Room> _allRooms = _roomReader.GetRooms();
            int _sheetCount = ((int)_allRooms.Count / 10) + 1;

            int _roomsPosition = 0;
            for (int i = 1; i<= _sheetCount; i++)
            {
                int _roomCountInThisSheet = Math.Min(10, _allRooms.Count - _roomsPosition);
                if (_roomCountInThisSheet == 0) break;

                string _sheetName = SHEET_NAME_ROOMS + (i > 1 ? $" {i}" : "");
                ExcelWorksheet _ws = CreateWorksheet(_sheetName);
                List<Room> _rooms = _allRooms.GetRange(_roomsPosition, _roomCountInThisSheet);
                _roomsPosition += _roomCountInThisSheet;

                int _startRow = 0;
                _rooms.ForEach(r =>
                {
                    CreateTable(_ws, r.Name, _startRow);
                    _startRow += TIME_SPANS.Length + 1 + TABLE_ROW_SPACING;
                });

                int _numberOfRooms = _rooms.Count;
                int _daySize = schedule.day_Hours * _numberOfRooms;

                foreach (KeyValuePair<CourseClass, int> it in courseClasses)
                {
                    int _pos = it.Value;
                    int _day = _pos / _daySize;
                    int _time = _pos % _daySize;
                    int _roomId = _time / schedule.day_Hours;
                    _startRow = _roomId * (TABLE_ROW_SPACING + 1 + TIME_SPANS.Length);
                    _time = (_time % schedule.day_Hours) + _startRow;
                    int _dur = it.Key.Duration;

                    CourseClass _cc = it.Key;
                    Room _room = Algorithm.Configuration.GetInstance.GetRoomById(_roomId);

                    string _groups_Name = "";
                    foreach (var group in _cc.StudentGroups)
                    {
                        _groups_Name += group.Name + "  ";
                    }
                    _groups_Name = _groups_Name.Trim();

                    _ws.Cells[_time + 1, _day + 1].Value = $"{_cc.Course.Name}\n{_cc.Prelector.Name}\n{_groups_Name}";
                    _ws.Cells[_time + 1, _day + 1].Style = _anyCellStyle;

                    // Merge Cells
                    try
                    {
                        if (_cc.Duration > 1) _ws.Cells.GetSubrangeAbsolute(_time + 1, _day + 1, _time + _dur, _day + 1).Merged = true;
                        _ws.Cells[_time + 1, _day + 1].Style = _anyCellStyle;
                    }
                    catch
                    {
                        _ws.Cells[_time + 1, _day + 1].Style.Font.Color = Color.DarkRed;

                    }
                }
            }
        }

        private void SaveGroupsSchedule(Schedule schedule, List<KeyValuePair<CourseClass, int>> courseClasses)
        {
            List<StudentGroup> _allGroups = _studentGroupReader.GetStudentGroups();
            int _sheetCount = ((int)_allGroups.Count / 10) + 1;

            int _groupsPosition = 0;
            for (int i = 1; i <= _sheetCount; i++)
            {
                int _groupCountInThisSheet = Math.Min(10, _allGroups.Count - _groupsPosition);
                if (_groupCountInThisSheet == 0) break;

                string _sheetName = SHEET_NAME_GROUPS + (i > 1 ? $" {i}" : "");
                ExcelWorksheet _ws = CreateWorksheet(_sheetName);
                List<StudentGroup> _groups = _allGroups.GetRange(_groupsPosition, _groupCountInThisSheet);
                _groupsPosition += _groupCountInThisSheet;

                int _startRow = 0;
                _groups.ForEach(r =>
                {
                    CreateTable(_ws, r.Name, _startRow);
                    _startRow += TIME_SPANS.Length + 1 + TABLE_ROW_SPACING;
                });

                int _numberOfRooms = Algorithm.Configuration.GetInstance.GetNumberOfRooms();
                int _daySize = schedule.day_Hours * _numberOfRooms;

                _groups.ForEach(group =>
                {

                    foreach (KeyValuePair<CourseClass, int> it in courseClasses.Where(cc => cc.Key.StudentGroups.Contains(group)).ToList())
                    {
                        int _pos = it.Value;
                        int _day = _pos / _daySize;
                        int _time = _pos % _daySize;
                        int _roomId = _time / schedule.day_Hours;
                        _startRow = _groups.IndexOf(group) * (TABLE_ROW_SPACING + 1 + TIME_SPANS.Length);
                        _time = (_time % schedule.day_Hours) + _startRow;
                        int _dur = it.Key.Duration;

                        CourseClass _cc = it.Key;
                        Room _room = Algorithm.Configuration.GetInstance.GetRoomById(_roomId);

                        string groups_Name = "";
                        foreach (var gs in _cc.StudentGroups)
                        {
                            groups_Name += gs.Name + "  ";
                        }
                        groups_Name = groups_Name.Trim();

                        _ws.Cells[_time + 1, _day + 1].Value = $"{_cc.Course.Name}\n{_cc.Prelector.Name}\n{_room.Name}";
                        _ws.Cells[_time + 1, _day + 1].Style = _anyCellStyle;

                    // Merge Cells
                    try
                        {
                            if (_cc.Duration > 1) _ws.Cells.GetSubrangeAbsolute(_time + 1, _day + 1, _time + _dur, _day + 1).Merged = true;
                            _ws.Cells[_time + 1, _day + 1].Style = _anyCellStyle;
                        }
                        catch
                        {
                            _ws.Cells[_time + 1, _day + 1].Style.Font.Color = Color.DarkRed;

                        }
                    }
                });
            }
        }

        private void SavePrelectorsSchedule(Schedule schedule, List<KeyValuePair<CourseClass, int>> courseClasses)
        {
            List<Prelector> _allPrelectors = _prelectorReader.GetPrelectors();
            int _sheetCount = ((int)_allPrelectors.Count / 10) + 1;

            int _prelectorsPosition = 0;
            for (int i = 1; i <= _sheetCount; i++)
            {
                int _prelectorCountInThisSheet = Math.Min(10, _allPrelectors.Count - _prelectorsPosition);
                if (_prelectorCountInThisSheet == 0) break;

                string _sheetName = SHEET_NAME_PRELECTORS + (i > 1 ? $" {i}" : "");
                ExcelWorksheet _ws = CreateWorksheet(_sheetName);
                List<Prelector> _prelectors = _allPrelectors.GetRange(_prelectorsPosition, _prelectorCountInThisSheet);
                _prelectorsPosition += _prelectorCountInThisSheet;
                
                int _startRow = 0;
                _prelectors.ForEach(p =>
                {
                    CreateTable(_ws, p.Name, _startRow);
                    _startRow += TIME_SPANS.Length + 1 + TABLE_ROW_SPACING;
                });

                int _numberOfRooms = Algorithm.Configuration.GetInstance.GetNumberOfRooms();
                int _daySize = schedule.day_Hours * _numberOfRooms;

                _prelectors.ForEach(prof =>
                {

                    foreach (KeyValuePair<CourseClass, int> it in courseClasses.Where(cc => cc.Key.Prelector.Equals(prof)).ToList())
                    {
                        int _pos = it.Value;
                        int _day = _pos / _daySize;
                        int _time = _pos % _daySize;
                        int _roomId = _time / schedule.day_Hours;
                        _startRow = _prelectors.IndexOf(prof) * (TABLE_ROW_SPACING + 1 + TIME_SPANS.Length);
                        _time = (_time % schedule.day_Hours) + _startRow;
                        int _dur = it.Key.Duration;

                        CourseClass _cc = it.Key;
                        Room _room = Algorithm.Configuration.GetInstance.GetRoomById(_roomId);

                        string _groups_Name = "";
                        foreach (var gs in _cc.StudentGroups)
                        {
                            _groups_Name += gs.Name + "  ";
                        }
                        _groups_Name = _groups_Name.Trim();

                        _ws.Cells[_time + 1, _day + 1].Value = $"{_cc.Course.Name}\n{_room.Name}\n{_groups_Name}";
                        _ws.Cells[_time + 1, _day + 1].Style = _anyCellStyle;

                    // Merge Cells
                    try
                        {
                            if (_cc.Duration > 1) _ws.Cells.GetSubrangeAbsolute(_time + 1, _day + 1, _time + _dur, _day + 1).Merged = true;
                            _ws.Cells[_time + 1, _day + 1].Style = _anyCellStyle;
                        }
                        catch
                        {
                            _ws.Cells[_time + 1, _day + 1].Style.Font.Color = Color.DarkRed;
                        }
                    }
                });
            }
        }

        private ExcelWorksheet CreateWorksheet(string name)
        {
            if (_ef.Worksheets.Count == 5)
            {
                if (!PATH_EXCEL_OUTPUT_DATA.EndsWith("/")) PATH_EXCEL_OUTPUT_DATA = PATH_EXCEL_OUTPUT_DATA + "/";
                _ef.Save($"{PATH_EXCEL_OUTPUT_DATA}{FILENAME_OUTPUT}");

                _excelFileCount += 1;
                _ef = new ExcelFile();
                FILENAME_OUTPUT = FILENAME_OUTPUT + $" ({_excelFileCount})";
            }

            ExcelWorksheet ws = _ef.Worksheets.Add(name);

            ws.Columns[0].Width = 30 * 226;
            ws.Columns[1].Width = 28 * 226;
            ws.Columns[2].Width = 28 * 226;
            ws.Columns[3].Width = 28 * 226;
            ws.Columns[4].Width = 28 * 226;
            ws.Columns[5].Width = 28 * 226;
            ws.Columns[6].Width = 28 * 226;

            return ws;
        }

        private void CreateTable(ExcelWorksheet ws, string header, int startRow)
        {
            #region Table Header

            ws.Cells[0 + startRow, 0].Value = header;
            ws.Cells[0 + startRow, 0].Style = _tableTitleStyle;

            #endregion

            #region First row

            ws.Rows[0 + startRow].Height = (5 * 226) / 2;

            for (int i = 1; i <= HEADER_TEXTS.Length; i++)
            {
                ws.Cells[0 + startRow, i].Value = HEADER_TEXTS[0, i - 1];
                ws.Cells[0 + startRow, i].Style = _headerStyle;
            }

            #endregion

            #region first column

            for (int i = 1; i <= TIME_SPANS.Length; i++)
            {
                ws.Cells[i + startRow, 0].Value = TIME_SPANS[0, i - 1];
                ws.Cells[i + startRow, 0].Style = _headerStyle;
                ws.Rows[i + startRow].Height = 4 * 226;
            }

            #endregion
        }

        #endregion
    }
}
