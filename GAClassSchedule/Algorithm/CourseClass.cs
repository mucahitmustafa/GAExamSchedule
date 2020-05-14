using System;
using System.Collections.Generic;

namespace GAClassSchedule.Algorithm
{
    public class CourseClass
    {

        public int ID { get; set; }
        public int StudentCount { get; set; }
        public bool RequiresLab { get; set; }
        public int Duration { get; set; }
        public Course Course { get; set; }
        public Prelector Prelector { get; set; }
        public List<StudentGroup> StudentGroups { get; set; }


        public CourseClass()
        {

        }

        public CourseClass(Prelector prelector, Course course, List<StudentGroup> groups, bool requiresLab,
            int duration, int id)
        {
            Prelector = prelector;
            Course = course;
            StudentGroups = groups;
            RequiresLab = requiresLab;
            Duration = duration;
            ID = id;

            StudentCount = 0;
            Prelector.AddCourseClass(this);

            foreach (StudentGroup group in groups)
            {
                group.AddCourseClass(this);
                StudentGroups.Add(group);
                StudentCount += group.StudentCount;
            }
        }

        
        public bool GroupsOverlap(CourseClass courseClass)
        {
            foreach (StudentGroup group1 in StudentGroups)
            {
                foreach (StudentGroup group2 in courseClass.StudentGroups)
                {
                    if (group1 == group2) return true;
                }
            }
            return false;
        }

        
        public bool SequentialGroupsOverlap(CourseClass courseClass)
        {
            foreach (StudentGroup group1 in StudentGroups)
            {
                foreach (StudentGroup group2 in courseClass.StudentGroups)
                {
                    if (group1 == group2 || ((group1.Branch == group2.Branch) && (Math.Abs(group1.Degree - group2.Degree) == 1))) return true;
                }
            }
            return false;
        }

        
        public bool PrelectorOverlaps(CourseClass courseClass)
        {
            return Prelector == courseClass.Prelector;
        }
    }
}
