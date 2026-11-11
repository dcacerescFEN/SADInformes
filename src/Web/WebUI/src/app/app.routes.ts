import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth-guard';
import { POSTGRADUATE_REPORTS } from './postgraduate/postgraduate-catalogue';
import { ACADEMIC_ENROLLMENT_REPORTS } from './academic-enrollment/academic-enrollment-catalogue';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: '/academic-enrollment/electives-without-common-plan',
  },
  ...POSTGRADUATE_REPORTS.map(report => ({
    path: 'postgraduate/' + report.slug,
    canActivate: [authGuard],
    data: { report },
    loadComponent: () => report.slug === 'mass-enrollment'
      ? import('./postgraduate/mass-enrollment/mass-enrollment').then((module) => module.MassEnrollment)
      : report.slug === 'exit-survey-enrollment'
      ? import('./postgraduate/exit-survey-enrollment/exit-survey-enrollment').then((module) => module.ExitSurveyEnrollment)
      : report.slug === 'exit-survey-statistics'
      ? import('./postgraduate/exit-survey-statistics/exit-survey-statistics').then((module) => module.ExitSurveyStatistics)
      : report.slug === 'entry-survey-statistics'
      ? import('./postgraduate/entry-survey-statistics/entry-survey-statistics').then((module) => module.EntrySurveyStatistics)
      : report.slug === 'course-teacher-evaluation'
      ? import('./postgraduate/course-teacher-evaluation/course-teacher-evaluation').then((module) => module.CourseTeacherEvaluation)
      : report.slug === 'personal-data'
      ? import('./postgraduate/personal-data/personal-data').then((module) => module.PersonalData)
      : report.slug === 'thesis-registration'
      ? import('./postgraduate/thesis-registration/thesis-registration').then((module) => module.ThesisRegistration)
      : report.slug === 'iss-statistics'
      ? import('./postgraduate/iss-statistics/iss-statistics').then((module) => module.IssStatistics)
      : report.slug === 'alumni-survey-statistics'
      ? import('./postgraduate/alumni-survey-statistics/alumni-survey-statistics').then((module) => module.AlumniSurveyStatistics)
      : report.slug === 'afe-report'
      ? import('./postgraduate/afe-report/afe-report').then((module) => module.AfeReport)
      : report.slug === 'undergraduate-licenciatura'
      ? import('./postgraduate/licenciatura/licenciatura').then((module) => module.Licenciatura)
      : report.slug === 'course-activities'
      ? import('./postgraduate/course-activities/course-activities').then((module) => module.CourseActivities)
      : report.slug === 'nce'
      ? import('./postgraduate/nce/nce').then((module) => module.Nce)
      : report.slug === 'basic-right-payment'
      ? import('./postgraduate/basic-right-payment/basic-right-payment').then((module) => module.BasicRightPayment)
      : report.slug === 'weighted-ranking'
      ? import('./postgraduate/weighted-ranking/weighted-ranking').then((module) => module.WeightedRanking)
      : report.slug === 'enrolled-students'
      ? import('./postgraduate/enrolled-students/enrolled-students').then((module) => module.EnrolledStudents)
      : report.slug === 'teacher-validities'
      ? import('./postgraduate/teacher-validities/teacher-validities').then((module) => module.TeacherValidities)
      : report.slug === 'area-courses'
      ? import('./postgraduate/area-courses/area-courses').then((module) => module.AreaCourses)
      : report.slug === 'sad-sigma'
      ? import('./postgraduate/sad-sigma/sad-sigma').then((module) => module.SadSigma)
      : report.slug === 'thesis-defenses'
      ? import('./postgraduate/thesis-defenses/thesis-defenses').then((module) => module.ThesisDefenses)
      : report.slug === 'sigma-tutorships'
      ? import('./postgraduate/ayudantia-sigma/ayudantia-sigma').then((module) => module.AyudantiaSigma)
      : report.slug === 'sad-vti-updates'
      ? import('./postgraduate/sad-vti-updates/sad-vti-updates').then((module) => module.SadVtiUpdates)
      : report.slug === 'student-requests'
      ? import('./postgraduate/student-requests/student-requests').then((module) => module.StudentRequests)
      : report.slug === 'courses-by-students'
      ? import('./postgraduate/courses-by-students/courses-by-students').then((module) => module.CoursesByStudents)
      : report.slug === 'unanswered-teacher-survey'
      ? import('./postgraduate/unanswered-teacher-survey/unanswered-teacher-survey').then((module) => module.UnansweredTeacherSurvey)
      : report.slug === 'teacher-courses'
      ? import('./postgraduate/teacher-courses/teacher-courses').then((module) => module.TeacherCourses)
      : report.slug === 'teaching-schedule'
      ? import('./postgraduate/teaching-schedule/teaching-schedule').then((module) => module.TeachingSchedule)
      : report.available
        ? import('./postgraduate/teacher-survey/teacher-survey').then((module) => module.TeacherSurvey)
        : import('./postgraduate/postgraduate-placeholder').then((module) => module.PostgraduatePlaceholder),
  })),
  ...ACADEMIC_ENROLLMENT_REPORTS.filter(report => !report.routePath).map(report => ({
    path: 'academic-enrollment/' + report.slug,
    canActivate: [authGuard],
    data: { report },
    loadComponent: () => report.slug === 'electives-without-common-plan'
      ? import('./academic-enrollment/electives-without-common-plan/electives-without-common-plan')
        .then((module) => module.ElectivesWithoutCommonPlan)
      : report.slug === 'students-without-priority'
        ? import('./academic-enrollment/students-without-priority/students-without-priority')
          .then((module) => module.StudentsWithoutPriority)
        : report.slug === 'postulation-results-by-course'
          ? import('./academic-enrollment/postulation-results-by-course/postulation-results-by-course')
            .then((module) => module.PostulationResultsByCourse)
          : import('./postgraduate/postgraduate-placeholder').then((module) => module.PostgraduatePlaceholder),
  })),
  { path: '**', redirectTo: '/academic-enrollment/electives-without-common-plan' },
];
