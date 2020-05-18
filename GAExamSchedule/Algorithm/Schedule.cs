﻿using GAExamSchedule.SpannedDataGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GAExamSchedule.Algorithm
{
    #region Classes

    public class Schedule
    {
        #region Constants and Fields

        public const int DAY_HOURS = 9;
        public const int DAYS_COUNT = 5;
        public int day_Hours { get { return DAY_HOURS; } }

        private const int NUMBER_OF_SCORES = 50;

        public int NumberOfCrossoverPoints { get; set; }
        public int MutationSize { get; set; }
        public int CrossoverProbability { get; set; }
        public int MutationProbability { get; set; }
        public float Fitness { get; set; }
        public bool[] Criteria { get; set; }

        List<CourseClass>[] _slots;
        Dictionary<CourseClass, int> _classes = new Dictionary<CourseClass, int>();
        public Dictionary<CourseClass, int> GetClasses() { return _classes; }

        #endregion

        #region Constructors

        public Schedule(int numberOfCrossoverPoints, int mutationSize, int crossoverProbability, int mutationProbability)
        {
            MutationSize = mutationSize;
            NumberOfCrossoverPoints = numberOfCrossoverPoints;
            CrossoverProbability = crossoverProbability;
            MutationProbability = mutationProbability;
            Fitness = 0;

            _slots = new List<CourseClass>[(DAYS_COUNT * DAY_HOURS * Configuration.GetInstance.GetNumberOfRooms())];
            for (int p = 0; p < (DAYS_COUNT * DAY_HOURS * Configuration.GetInstance.GetNumberOfRooms()); p++)
                _slots[p] = new List<CourseClass>();

            Criteria = new bool[(Configuration.GetInstance.GetNumberOfCourseClasses() * NUMBER_OF_SCORES)];
        }

        public Schedule(Schedule c, bool setupOnly)
        {
            if (setupOnly)
            {
                _slots = new List<CourseClass>[(DAYS_COUNT * DAY_HOURS * Configuration.GetInstance.GetNumberOfRooms())];
                for (int ptr = 0; ptr < (DAYS_COUNT * DAY_HOURS * Configuration.GetInstance.GetNumberOfRooms()); ptr++)
                    _slots[ptr] = new List<CourseClass>();

                Criteria = new bool[(Configuration.GetInstance.GetNumberOfCourseClasses() * NUMBER_OF_SCORES)];
            }
            else
            {
                _slots = c._slots;
                _classes = c._classes;
                Criteria = c.Criteria;
                Fitness = c.Fitness;
            }

            NumberOfCrossoverPoints = c.NumberOfCrossoverPoints;
            MutationSize = c.MutationSize;
            CrossoverProbability = c.CrossoverProbability;
            MutationProbability = c.MutationProbability;
        }

        #endregion

        #region Public Methods

        public Schedule MakeCopy(bool setupOnly)
        {
            return new Schedule(this, setupOnly);
        }

        public Schedule MakeNewFromPrototype()
        {
            Schedule newChromosome = new Schedule(this, true);

            List<CourseClass> _ccS = Configuration.GetInstance.GetCourseClasses();
            foreach (CourseClass _cc in _ccS)
            {
                int _numberOfRooms = Configuration.GetInstance.GetNumberOfRooms();
                int _dur = _cc.Duration;
                Random _rnd = new Random();
                int _day = _rnd.Next() % DAYS_COUNT;
                int _room = _rnd.Next() % _numberOfRooms;
                int _time = _rnd.Next() % (DAY_HOURS + 1 - _dur);
                int _pos = (_day * _numberOfRooms * DAY_HOURS) + (_room * DAY_HOURS) + _time;

                for (int i = _dur - 1; i >= 0; i--)
                    newChromosome._slots[_pos + i].Add(_cc);

                if (newChromosome._classes == null)
                {
                    newChromosome._classes = new Dictionary<CourseClass, int>();
                }
                newChromosome._classes.Add(_cc, _pos);
            }

            newChromosome.CalculateFitness();

            return newChromosome;
        }

        public Schedule Crossover(Schedule parent2)
        {
            Random _rnd = new Random();

            if (_rnd.Next() % 100 > CrossoverProbability)
                return new Schedule(this, false);

            Schedule _child = new Schedule(this, true);

            int _size = _classes.Count;
            bool[] cp = new bool[_size];

            for (int i = NumberOfCrossoverPoints; i > 0; i--)
            {
                while (true)
                {
                    int p = _rnd.Next() % _size;
                    if (!cp[p])
                    {
                        cp[p] = true;
                        break;
                    }
                }
            }

            List<KeyValuePair<CourseClass, int>> _parent1ccs = _classes.ToList<KeyValuePair<CourseClass, int>>();

            List<KeyValuePair<CourseClass, int>> _parent2ccs = parent2._classes.ToList<KeyValuePair<CourseClass, int>>();

            bool first = (_rnd.Next() % 2 == 0);
            for (int i = 0; i < _size; i++)
            {
                if (first)
                {
                    _child._classes.Add(_parent1ccs[i].Key, _parent1ccs[i].Value);
                    for (int j = _parent1ccs[i].Key.Duration - 1; j >= 0; j--)
                        _child._slots[_parent1ccs[i].Value + j].Add(_parent1ccs[i].Key);
                }
                else
                {
                    _child._classes.Add(_parent2ccs[i].Key, _parent2ccs[i].Value);
                    for (int j = _parent2ccs[i].Key.Duration - 1; j >= 0; j--)
                        _child._slots[_parent2ccs[i].Value + j].Add(_parent2ccs[i].Key);
                }

                if (cp[i])
                    first = !first;
            }

            _child.CalculateFitness();
            return _child;
        }

        public void Mutation()
        {
            Random _rnd = new Random();

            if (_rnd.Next() % 100 > MutationProbability)
                return;

            int _numberOfClasses = _classes.Count;
            int _size = _slots.Length;

            for (int i = MutationSize; i > 0; i--)
            {
                int mpos = _rnd.Next() % _numberOfClasses;
                KeyValuePair<CourseClass, int> it = _classes.ToList<KeyValuePair<CourseClass, int>>()[mpos];

                int pos1 = it.Value;
                CourseClass cc1 = it.Key;

                int nr = Configuration.GetInstance.GetNumberOfRooms();
                int dur = cc1.Duration;
                int day = _rnd.Next() % DAYS_COUNT;
                int room = _rnd.Next() % nr;
                int time = _rnd.Next() % (DAY_HOURS + 1 - dur);
                int pos2 = day * nr * DAY_HOURS + room * DAY_HOURS + time;

                for (int j = dur - 1; j >= 0; j--)
                {
                    List<CourseClass> cl = _slots[pos1 + j];
                    foreach (CourseClass It in cl)
                    {
                        if (It == cc1)
                        {
                            cl.Remove(It);
                            break;
                        }
                    }

                    _slots[pos2 + j].Add(cc1);
                }

                _classes[cc1] = pos2;
            }
            
            CalculateFitness();
        }

        public void CalculateFitness()
        {
            int _score = 0;

            int _numberOfRooms = Configuration.GetInstance.GetNumberOfRooms();
            int _daySize = DAY_HOURS * _numberOfRooms;

            int _ci = 0;

            foreach (KeyValuePair<CourseClass, int> it in _classes.ToList())
            {
                int _pos = it.Value;
                int _day = _pos / _daySize;
                int _time = _pos % _daySize;
                int _roomId = _time / DAY_HOURS;
                _time = _time % DAY_HOURS;
                int _dur = it.Key.Duration;

                CourseClass _cc = it.Key;
                Room _room = Configuration.GetInstance.GetRoomById(_roomId);

                #region Score 1 (check for room overlapping of classes)  [+10]

                bool _overlapping = false;
                for (int i = _dur - 1; i >= 0; i--)
                {
                    if (_slots[_pos + i].Count > 1)
                    {
                        _overlapping = true;
                        break;
                    }
                }

                if (!_overlapping)
                    _score += 10;

                Criteria[_ci + 0] = !_overlapping;

                #endregion

                #region Score 2 (does current room have enough seats)  [+10]

                Criteria[_ci + 1] = _room.Capacity >= _cc.StudentCount;
                if (Criteria[_ci + 1])
                    _score += 10;

                #endregion

                #region Score 3 (does current room fair)  [+5]

                Criteria[_ci + 2] = _cc.RequiresLab.Equals(_room.IsLab);
                if (Criteria[_ci + 2])
                    _score += 5;

                #endregion

                #region Score 4 and 5 and 6 (check overlapping of classes for prelectors and student groups and sequential student groups)  [+8][+8][+4]

                bool _pre = false, _gro = false, _seqGro = false;
                for (int i = _numberOfRooms, t = (_day * _daySize + _time); i > 0; i--, t += DAY_HOURS)
                {
                    for (int j = _dur - 1; j >= 0; j--)
                    {
                        List<CourseClass> cl = _slots[t + j];
                        foreach (CourseClass it_cc in cl)
                        {
                            if (_cc != it_cc)
                            {
                                if (!_pre && _cc.PrelectorOverlaps(it_cc))
                                    _pre = true;

                                if (!_gro && _cc.GroupsOverlap(it_cc))
                                    _gro = true;

                                if (!_seqGro && _cc.SequentialGroupsOverlap(it_cc))
                                    _seqGro = true;

                                if (_pre && _gro && _seqGro)
                                    goto total_overlap;
                            }
                        }
                    }
                }

            total_overlap:

                if (!_pre)
                    _score += 8;
                Criteria[_ci + 3] = !_pre;

                if (!_gro)
                    _score += 8;
                Criteria[_ci + 4] = !_gro;

                if (!_seqGro)
                    _score += 4;
                Criteria[_ci + 5] = !_seqGro;

                #endregion

                #region Score 7 (check course limit in one day for student groups)  [+3]

                bool _limitExceeded = false;
                foreach (StudentGroup group in _cc.StudentGroups)
                {
                    int hourInDay = 0;
                    for (int j = 0; j < DAY_HOURS; j++)
                    {
                        List<CourseClass> courseClassesInTime = _slots[_day * _daySize + j];
                        foreach (CourseClass cc_it in courseClassesInTime)
                        {
                            if (cc_it.StudentGroups.Contains(group)) hourInDay++;
                        }
                    }

                    if (hourInDay > group.MaxHourInDay)
                    {
                        _limitExceeded = true;
                        break;
                    }
                }

                if (!_limitExceeded)
                {
                    _score += 3;
                }
                Criteria[_ci + 6] = !_limitExceeded;


                #endregion

                #region Score 8 (check this class day in prelector schedule table)  [+2]

                Criteria[_ci + 7] = true;
                for (int i = 0; i < _dur; i++)
                {
                    if (!_cc.Prelector.ScheduleDays[_day])
                    {
                        Criteria[_ci + 7] = false;
                        break;
                    }
                }
                if (Criteria[_ci + 7])
                    _score += 2;

                #endregion

                _ci += NUMBER_OF_SCORES;
            }

            Fitness = (float)_score / (Configuration.GetInstance.GetNumberOfCourseClasses() * NUMBER_OF_SCORES);
        }


        #endregion


        public class ScheduleObserver
        {
            private static CreateDataGridViews _window;

            public void NewBestChromosome(Schedule newChromosome, bool showGraphical)
            {
                showGraphical = newChromosome.Fitness > 0.9;
                if (_window.DgvList.Count > 0)
                    _window.SetSchedule(newChromosome, showGraphical);
            }

            public void EvolutionStateChanged(AlgorithmState newState)
            {
                if (_window != null)
                    _window.SetNewState(newState);
            }

            public void SetWindow(CreateDataGridViews window)
            { _window = window; }
        }
    }

    #endregion
}