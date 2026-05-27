Design Version 1 of a premium enterprise web application for AIMBYS Solutions using **.NET Core + Bootstrap**. This is a large-scale, government-grade **paper generation, examination, evaluation, and analytics platform** inspired by mature systems like PARAKH ITMS in its business logic and hierarchy, but with a more modern, polished, high-utility enterprise UI.

The design must feel like a real institutional product used in government, schools, colleges, boards, and enterprise education networks. It should not look AI-generated, over-glossy, or overly gradient-based. Avoid generic SaaS visuals. Use **realistic enterprise graphics, structured layouts, data-heavy screens, operational dashboards, and productivity-first UI patterns**.

The system is owned and controlled by **AIMBYS Solutions**.

==================================================
CORE HIERARCHY / BUSINESS LOGIC
===============================

Use a strict lifecycle and permission model:

1. AIMBYS Super Admin
2. Institute / Organization Admin
3. Teacher / Paper Generator / Examiner
4. Student / Candidate

Rules:

* Upper layers fully control, monitor, approve, and analyze lower layers.
* AIMBYS controls institutes.
* Institutes manage teachers, students, departments, classes, subjects, permissions, and reports.
* Teachers create question banks, papers, assessments, evaluate responses, and analyze student performance.
* Students take exams, submit answers, view results, and track progress.
* Every action must respect role-based access, approval flow, audit logs, and tenant isolation.

The platform must feel like a **national-scale educational ERP and assessment OS**.

==================================================
DESIGN STYLE
============

Create a premium design language that feels:

* institutional
* trusted
* enterprise-ready
* government-approved
* operationally mature
* highly readable
* workflow-driven
* visually rich but not flashy

Use:

* strong grid systems
* real data visualization
* serious dashboard aesthetics
* soft depth and shadows
* professional iconography
* polished tables and forms
* clear hierarchy
* subtle motion cues
* minimal but powerful use of gradients
* actual enterprise visuals, not decorative noise

Avoid:

* excessive glassmorphism
* cartoonish illustrations
* random floating elements
* startup-style gimmicks
* overuse of neon gradients
* UI that looks fake or template-like

==================================================
PRIMARY PRODUCT GOAL
====================

This application is a complete **paper generation and analytics ecosystem** where institutions can:

* create and manage user hierarchies
* build question banks
* generate papers manually or automatically
* support multiple question types
* conduct exams
* evaluate answers
* review performance
* generate analytics
* manage approvals
* export reports
* maintain compliance and auditability

The design must clearly show that this is a robust enterprise system suitable for serious procurement and large-scale deployment.

==================================================
MAIN MODULES
============

1. AIMBYS Super Admin Panel

* institute onboarding and approval
* tenant management
* license/subscription control
* global analytics
* audit logs
* security monitoring
* system health dashboard
* user activity monitoring
* regional/state performance overview
* white-label branding management
* backup and recovery oversight
* notification broadcasts
* compliance monitoring

2. Institute Admin Panel

* create departments, classes, batches, subjects, teachers, and students
* assign roles and permissions
* approve teacher workflows
* manage academic year and exam calendar
* manage question banks
* assign papers and reviewers
* monitor faculty workload
* publish results
* view institute analytics
* export reports
* manage institute branding and settings

3. Teacher / Paper Generator Panel

* create and manage question banks
* generate papers manually and automatically
* use blueprint-based paper creation
* choose subject, topic, chapter, difficulty, marks, and timing
* assign co-teachers / reviewers
* create exams and assignments
* evaluate answers manually where required
* manage rubric-based marking
* review student performance
* compare batches and subjects
* monitor question quality
* support moderation and recheck workflow

4. Student Panel

* login and profile
* exam list
* assigned papers
* quiz/exam taking interface
* submit answers
* view scores and result sheets
* analyze performance
* track strengths and weaknesses
* see leaderboards
* view certificates/transcripts
* receive notifications
* access study recommendations

==================================================
QUESTION / ASSESSMENT TYPES
===========================

The UI must support these question formats:

* MCQ
* multi-select MCQ
* true/false
* fill in the blanks
* TITA / typed answers
* descriptive answers
* rich text answers
* program-based questions
* coding questions with IDE
* debugging questions
* SQL questions
* case-study questions
* file-upload answers
* image-based answers
* diagram/label-based questions
* mathematical/equation-based questions

Each question type must have a matching UI state for:

* create
* edit
* preview
* review
* evaluate
* publish
* attempt
* report

==================================================
QUESTION PAPER DASHBOARD / PAPER GENERATION
===========================================

Design a powerful paper-generation workspace with:

* blueprint builder
* question bank browser
* question tagging
* chapter/subject/difficulty filters
* marks distribution control
* time duration control
* language selection
* competency tagging
* reviewer assignment
* approval workflow
* manual paper assembly
* auto-generation mode
* mixed question-type paper builder
* question order control
* duplicate detection
* paper version history
* export and print preview

Include a rich text editor for descriptive and TITA questions, with:

* formatting toolbar
* attachments
* inline media
* markdown-like editing support
* preview mode
* rubric hints
* manual checking interface

==================================================
CODING EXAM / IDE MODULE
========================

Create a modern code-question environment with:

* code editor
* language selector
* run button
* debug button
* console/output panel
* test case panel
* file tree area
* timer
* submission status
* plagiarism warning indicators
* execution results
* full-screen coding mode
* review workflow for teacher verification

==================================================
MANUAL EVALUATION SYSTEM
========================

Design a teacher grading area with:

* answer queue
* student response viewer
* split-screen paper and answer view
* rich text review
* annotation tools
* rubric-based scoring
* partial marking
* comment threads
* approval/recheck workflow
* co-teacher assignment
* marking progress tracker
* moderation status
* final publish state

==================================================
ANALYTICS & REPORTING
=====================

Create high-value enterprise analytics with:

* institute dashboard
* teacher workload dashboard
* student progress dashboard
* question quality analytics
* topic mastery analysis
* subject heatmaps
* performance trends
* accuracy charts
* difficulty analysis
* pass/fail rates
* comparison by class/subject/batch
* time-based trends
* completion rates
* attendance/exam participation insights
* export center
* PDF/Excel report generation UI
* certificate generation
* transcript/report card UI
* compliance and audit reports

Use realistic charts:

* line charts
* bar charts
* stacked charts
* radial charts
* heatmaps
* progress rings
* KPI cards
* tables with filters and bulk actions

==================================================
ACCESSIBILITY & USABILITY
=========================

The design must be accessibility-first:

* keyboard navigation
* clear focus states
* high contrast modes
* readable typography
* accessible color combinations
* screen-reader-friendly structure
* large click/tap targets
* logical tab order
* error and success messaging
* form validation states
* empty/loading/skeleton/error states
* multilingual-friendly layout
* low-bandwidth friendly UI concepts
* simple workflow steps for non-technical users

Make the platform easy for:

* administrators
* teachers
* students
* reviewers
* managers
* auditors

==================================================
RESPONSIVE DESIGN
=================

Design fully responsive versions for:

* desktop
* large desktop / widescreen monitors
* tablet
* mobile

Show how each major module adapts:

* multi-column desktop dashboards
* collapsible sidebar desktop/tablet navigation
* bottom navigation or condensed menu for mobile
* responsive tables
* stacked cards on small screens
* adaptable forms
* mobile-friendly data entry
* touch-friendly controls

==================================================
NAVIGATION & ENTERPRISE UX
==========================

Create a powerful enterprise navigation system:

* sidebar navigation
* top bar with global search
* quick actions
* notification center
* mega menu for modules
* breadcrumb hierarchy
* command palette
* role-based menu visibility
* tabbed pages for dense modules
* workflow stepper for complex processes
* shortcut access to recent tasks

==================================================
SECURITY / GOVERNANCE / TRUST
=============================

Show UI concepts for:

* secure login
* role-based access control
* approval states
* audit logs
* activity history
* document vault
* digital signature approval
* verification workflow
* backup status
* disaster recovery dashboard
* compliance tracking
* suspicious activity monitoring
* exam integrity indicators
* data retention controls

==================================================
ADDITIONAL ENTERPRISE FEATURES TO INCLUDE
=========================================

Add these concepts where useful to make the product robust and versatile:

* notification system
* internal messaging
* helpdesk / support tickets
* knowledge base
* training portal
* FAQ center
* calendar and scheduling
* task reminders
* question moderation lifecycle
* reviewer/moderator workflows
* department hierarchy
* class/batch management
* subject and syllabus management
* exam conflict detection
* workload balancing
* AI-based recommendations
* plagiarism detection
* biometric verification concept screens
* QR/barcode verification
* parent/guardian access concept
* public result verification portal
* integration settings
* API management UI
* import/export center
* template gallery
* white-label institute branding
* regional/state reporting
* subscription/license management
* data governance dashboard
* SLA/task monitoring
* operational status indicators

==================================================
TECHNOLOGY PRESENTATION STYLE
=============================

The design is for a **.NET Core web application using Bootstrap**. Reflect this in the UI structure through:

* mature admin dashboard patterns
* Bootstrap-friendly forms and tables
* enterprise grid behavior
* reusable card systems
* modal dialogs
* tabs, accordions, drawers, and side panels
* clean component hierarchy
* production-ready layout logic

Do not design it like a landing page only. Design it like a serious working software product.

==================================================
DELIVERABLES FOR FIGMA MAKE
===========================

Create:

* design system
* color palette
* typography scale
* spacing system
* icon style
* button system
* form components
* table components
* modal and drawer patterns
* card system
* chart components
* dashboard templates
* responsive page templates
* empty/loading/error states
* workflow diagrams
* role hierarchy screens
* high-fidelity screens
* interactive prototype flow

==================================================
CORE PAGES TO DESIGN
====================

Design high-fidelity screens for:

* login / auth
* super admin dashboard
* institute dashboard
* teacher dashboard
* student dashboard
* institute onboarding
* user management
* role and permission management
* question bank
* blueprint builder
* manual paper generation
* auto paper generation
* paper preview and print
* exam schedule
* answer evaluation
* coding IDE exam screen
* analytics dashboard
* leaderboard
* result and certificate pages
* notifications
* helpdesk
* settings
* audit logs
* export center
* compliance dashboard

==================================================
FINAL DESIGN GOAL
=================

The final product must feel like:

* a government-grade assessment platform
* a national-scale paper generation system
* a trusted institutional ERP
* a serious enterprise analytics product
* a premium .NET Core + Bootstrap web application

It must be polished, realistic, scalable, visually strong, and easy to convert into production code in the next phase.
