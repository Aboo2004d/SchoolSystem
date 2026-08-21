# توثيق نظام إدارة المدارس

> هذا الملف يصف الحالة الفعلية للمشروع بعد تحويل المصادقة إلى ASP.NET Core Identity، وتحويل معرفات المجال إلى GUID، وإضافة فصل الأدوار والملكية وRedis وبيانات اختبار الضغط.

| بيان الوثيقة | القيمة |
|---|---|
| الإصدار | 2.0 |
| آخر تحديث | 15 آب 2026 |
| النطاق | المعمارية، الأمان، البيانات، التشغيل، Redis، Seeders، وعقود API |
| حالة الوثيقة | جاهزة للتقديم والتسليم التقني |

## 1. نظرة عامة

نظام ويب لإدارة المدارس مبني بـ ASP.NET Core MVC. يغطي المدارس، المديرين، المعلمين، الطلاب، الصفوف، المواد، إسناد المعلمين، العلامات، الحضور، الملفات الشخصية، الصور، التصدير إلى Excel، الشهادات وتقارير الأخطاء.

التقنيات الأساسية:

- .NET 9 وASP.NET Core MVC.
- Entity Framework Core 9 وSQL Server.
- ASP.NET Core Identity بمفاتيح `Guid`.
- Redis عبر `IDistributedCache` وStackExchange.Redis.
- Razor Views وBootstrap وjQuery وDataTables.
- EPPlus لتصدير Excel.
- QuestPDF وشريط Amiri للشهادات العربية.
- MailKit للبريد الإلكتروني.
- AspNetCoreHero.Notyf للإشعارات.

## 2. بنية المشروع

| المسار | المسؤولية |
|---|---|
| `Program.cs` | التسجيل في DI، Identity، Redis، Session، Middleware، Migration وSeeders |
| `Data/` | DbContext، كيانات المجال، ApplicationUser وSeeders |
| `Controllers/` | صفحات MVC؛ وفي العلامات والحضور تبقى عمليات فتح الصفحات فقط |
| `Controllers/ApiController/` | نقاط JSON/AJAX للقراءة والكتابة والتصدير والتشخيص |
| `Models/` | ViewModels ونماذج الطلب والاستجابة |
| `Filters/` | التحقق من الأدوار وملكية الموارد |
| `Services/` | الحسابات، البريد، الجلسة المتوافقة، السجلات والشهادات |
| `Middlewares/` | التقاط الأخطاء وتسجيلها |
| `Helpers/` | تخزين واسترجاع GUID من Session |
| `Views/` | واجهات Razor حسب كل Controller |
| `Migrations/` | Migration أولية ومخطط SQL كامل |
| `wwwroot/` | CSS وJavaScript والصور والخطوط والمكتبات الأمامية |
| `PrivateImages/` | صور خاصة تُقرأ من Controller وليست static files مباشرة |

## 3. دورة تشغيل التطبيق

1. يحمّل الإعدادات وملف `appsetting.env` إن وجد.
2. يسجل DbContext على SQL Server.
3. يسجل Identity وCookie وسياسات كلمات المرور والقفل.
4. يسجل Session للتوافق القديم، وليس كمصدر صلاحيات.
5. يسجل Redis وخدمات المشروع والفلاتر.
6. ينفذ `MigrateAsync` على قاعدة البيئة الحالية.
7. ينشئ الأدوار الناقصة.
8. يشغل Seeder الأدمن الرئيسي فقط عندما يكون `SeedAdmin:Enabled=true` وإعداداته الآمنة مكتملة.
9. يشغل Seeder بيانات الضغط فقط في Development وعندما يكون `LoadTestSeed:Enabled=true`.
10. يبني Middleware pipeline ثم يربط route الافتراضي.

المسار الافتراضي:

```text
{controller=Home}/{action=Index}/{id?}
```

## 4. قاعدة البيانات والمعرفات

كل المفاتيح الأساسية والخارجية الخاصة بمجال النظام تستخدم `Guid` أو `Guid?`. لا يوجد تشفير للمعرفات في الروابط.

الأنواع التي بقيت أرقامًا صحيحة ليست معرفات سجلات:

- `IdNumber`: رقم الهوية الوطنية.
- الدرجات ومجموعها.
- أرقام الصفوف والشعب.
- عدادات Identity الداخلية مثل Claim IDs.

### الجداول الأساسية

| الجدول/الكيان | الغرض والعلاقات المهمة |
|---|---|
| `AspNetUsers` | مستخدمو Identity؛ المفتاح `Guid` وحقل `IsActive` |
| `AspNetRoles` | Admin وManager وTeacher وStudent؛ المفتاح `Guid` |
| `Menegar` | ملف الأدمن/مدير المدرسة؛ ارتباط اختياري one-to-one بالمستخدم |
| `Teacher` | ملف المعلم؛ one-to-one بالمستخدم، وانتماء اختياري لمدرسة |
| `Student` | ملف الطالب؛ one-to-one بالمستخدم، مدرسة وصف |
| `School` | المدرسة وحالتها وجنسها ومرحلتها وحدود الصفوف |
| `StatusSchool` | حالة المدرسة مثل Active |
| `Gender` | نوع المدرسة |
| `StageClass` | المرحلة، الرمز حرف واحد وفريد |
| `Branch` | الفرع، رمز الفرع حرف واحد |
| `TheClass` | الصف/الشعبة ومدرسته ومرحلته وفرعه |
| `Lectuer` | المادة الدراسية (الاسم التاريخي في الكود Lectuer) |
| `TeacherLectuerClass` | ربط المعلم بالمادة والصف والمدرسة |
| `StudentLectuerTeacher` | ربط الطالب بالمادة والمعلم والصف والمدرسة |
| `Grade` | علامات الطالب لمادة ومعلم وصف ومدرسة |
| `Attendance` | حضور الطالب مع المعلم والمادة والصف والمدرسة |
| `ProfileImage` | مسار صورة الملف الشخصي |
| `ErrorLog` | سجل أخطاء التطبيق |

### علاقات الملفات الشخصية

`ApplicationUserId` اختياري وفريد في `Menegar` و`Teacher` و`Student`. هذا يحقق one-to-one مع إبقاء ملف المجال منفصلًا عن Identity. لا يمكن ربط ملفين من النوع نفسه بالمستخدم ذاته.

### الحذف المنطقي

عدة كيانات تستخدم أعلامًا مثل `IsDeleted` و`IsDeletedStudent` و`IsDeletedSchool`. العمليات المعتادة يجب أن تستبعد السجلات المحذوفة منطقيًا بدل حذفها فعليًا.

## 5. Identity والمصادقة

```csharp
ApplicationUser : IdentityUser<Guid>
IdentityRole<Guid>
SystemSchoolDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
```

التدفقات المحولة إلى Identity:

- تسجيل الدخول بواسطة `SignInManager`.
- تسجيل الخروج.
- إنشاء الحساب وربطه بملف موجود.
- تغيير كلمة المرور.
- نسيان واستعادة كلمة المرور باستخدام tokens.
- إدارة المستخدم والأدوار بواسطة UserManager وRoleManager.

سياسة كلمة المرور الحالية:

- 10 محارف على الأقل.
- رقم وحرف صغير وحرف كبير ورمز خاص.
- بريد فريد.
- القفل بعد 5 محاولات فاشلة لمدة 15 دقيقة.

Cookie:

- HttpOnly وEssential.
- Secure دائمًا.
- SameSite=Lax.
- مدة 30 دقيقة مع SlidingExpiration.

`ApplicationClaimsPrincipalFactory` يضيف claim باسم `active`. المستخدم ذو `IsActive=false` لا يستطيع دخول المسارات المحمية. بعد تغيير الأدوار أو هذا الحقل يجب تسجيل الخروج والدخول لتجديد Cookie.

## 6. الأدوار والصلاحيات

| الدور | النطاق |
|---|---|
| `Admin` | الأدمن الأعلى؛ المدارس، حالات المدارس، سجلات الأخطاء والإدارة العامة |
| `Manager` | مدير مدرسة؛ بيانات مدرسته فقط |
| `Teacher` | بياناته وصفوفه ومواده وطلابه وعلاماتهم وحضورهم فقط |
| `Student` | ملفه ومواده وعلاماته وحضوره فقط |

`RoleNames.Normalize` يوحد الكتابة ويقبل الاسم التاريخي `menegar` كـManager.

### الحماية الافتراضية

`DefaultPolicy` و`FallbackPolicy` تطلبان مستخدمًا مسجلًا و`active=true`. أي Action غير موسوم يصبح خاصًا تلقائيًا. الصفحات العامة فقط تستخدم `[AllowAnonymous]`، مثل تسجيل الدخول واستعادة كلمة المرور والصفحة الرئيسية العامة.

### AuthorizeRoles

`AuthorizeRolesAttribute` يتحقق من Identity principal والأدوار بعد التطبيع. لا يقرأ الدور من Session.

### ملكية البيانات

`OwnershipAuthorizationFilter` يعمل عالميًا كطبقة دفاع إضافية:

- يتجاوز الأدمن الأعلى فحص النطاق.
- يطابق `teacherId` مع ملف المعلم الحالي.
- يسمح للمعلم بالطالب فقط إذا كان مربوطًا به.
- يتحقق من ملكية Grade وAttendance عند تمرير معرف عام.
- يطابق `studentId` مع الطالب الحالي.
- يقيد المدير بالمدرسة ويرفض معرف مدرسة/معلم/طالب/صف/مادة خارجها.

كما تستمر Controllers في استخدام شروط EF وخدمة `SessionValidatorService`. يجب المحافظة على الشرطين عند إضافة endpoint جديد: الدور المناسب + تصفية المورد حسب المستخدم أو المدرسة.

## 7. Session

Session موجودة مؤقتًا للتوافق مع Controllers القديمة وتحتوي مثلًا:

- `Id`
- `School`
- `Role`
- `UserName`
- `Name`

`SessionGuidExtensions` يخزن GUID بصيغة نصية معيارية. Session ليست مصدر التفويض؛ المصدر هو Identity claims وفحوص قاعدة البيانات.

## 8. Controllers والوظائف

- `AccountController`: الدخول، التسجيل، ربط الملف، الخروج، الاستعادة وتغيير كلمة المرور.
- `HomeController`: الصفحات العامة ولوحة الإدارة.
- `ProfileController`: عرض وتعديل الملف المرتبط بالمستخدم فقط.
- `SchoolController` و`StatusSchoolController`: إدارة عامة للأدمن الأعلى.
- `MenegarController`: شاشات مدير المدرسة وإدارة طلابها ومعلميها وصفوفها.
- `TeacherController`: شاشة المعلم، طلابه، إسناداته وشهادته.
- `StudentController`: شاشة الطالب وشهادته؛ بيانات الرسوم البيانية تأتي من `StudentApiController`.
- `TheClassController`: الصفوف وإسناد المعلمين.
- `LectuerController`: المواد وربط المعلمين والطلاب.
- `GradesController`: فتح صفحات العلامات؛ القراءة والحفظ والتعديل والحذف عبر `GradesApiController`.
- `AttendanceController`: فتح صفحات الحضور؛ القراءة والحفظ والتعديل والحذف عبر `AttendanceApiController`.
- `ExportDataController`: تصدير الطلاب والمعلمين والعلامات إلى Excel.
- `ImageController` و`ImageProfileController`: قراءة ورفع الصور الخاصة بأسماء ملفات آمنة.
- `ErrorLogsController`: عرض سجلات الأخطاء للأدمن الأعلى.
- مجلد `ApiController`: نسخ JSON/AJAX للعمليات السابقة وتشخيص Redis.

## 9. CSRF والأمان التطبيقي

- `AutoValidateAntiforgeryTokenAttribute` مسجل عالميًا للطلبات غير الآمنة.
- النماذج الحساسة تحمل `ValidateAntiForgeryToken` أيضًا حيث يلزم.
- GUID يقلل قابلية التخمين لكنه ليس بديلًا عن الملكية والتفويض.
- الصور الخاصة تتحقق من اسم الملف وتمنع `..` وpath traversal.
- الأسرار لا توضع في Git أو appsettings؛ تستخدم User Secrets أو متغيرات البيئة.
- لا يوجد `EncryptionHelper` ولا `EncryptionSettings` ولا تشفير يدوي للمعرفات.

## 10. Redis والكاش

الإعداد الحالي يتصل بـ:

```text
localhost:6379
InstanceName: SchoolApp_
```

الكاش الحالي يركز على قوائم المدرسة:

```text
SchoolApp_Students_School_{SchoolGuid}
SchoolApp_Teachers_School_{SchoolGuid}
```

يتم إبطال المفاتيح عند عمليات تعديل ذات صلة. استجابات القوائم تضيف:

- `X-Cache: MISS` عند الاستعلام من SQL والتخزين.
- `X-Cache: HIT` عند القراءة من Redis.
- `X-Cache-Key` للمفتاح المنطقي قبل prefix.

تشخيص محمي للأدمن:

```text
GET /api/diagnostics/redis
```

يعيد الاتصال، عدد المفاتيح، النوع، TTL والحجم. إن لم يكن `redis-cli` مثبتًا استخدم هذا endpoint أو RedisInsight.

## 11. Seeders

### الأدمن الرئيسي

`IdentityDataSeeder` ينشئ/يحدث الأدمن ويربطه بملف Menegar دون مدرسة. كلمة المرور مطلوبة من User Secrets:

```powershell
dotnet user-secrets set "SeedAdmin:Password" "<strong-password>"
```

### بيانات الضغط

`LoadTestDataSeeder` محظور برمجيًا خارج Development، وداخل Development لا يعمل إلا عند `LoadTestSeed:Enabled=true`. ينشئ بيانات مترابطة بحفظ مرحلي و`AddRange`:

- مدارس ومديرين ومعلمين وصفوف ومواد وطلاب.
- حسابات Identity حقيقية وأدوار حقيقية.
- إسنادات المعلمين والطلاب.
- علامات وحضور.

النمط:

- `manager1`, `manager2`, ...
- `teacher1`, `teacher2`, ...
- `stu1`, `stu2`, ...

التفعيل:

```powershell
dotnet user-secrets set "LoadTestSeed:Password" "LoadTest2026!Aa"
dotnet user-secrets set "LoadTestSeed:Enabled" "true"
dotnet run -- --seed-only
```

أوقف `dotnet watch` قبل الأمر. يعمل الأمر كعملية إدخال يدوية مستقلة، يطبع تقدم كل دفعة، ثم يغلق عند الاكتمال. بعد ذلك عطّل `LoadTestSeed:Enabled` وشغّل الموقع طبيعيًا.

الأحجام الافتراضية: 3 مدارس، مديرين/مدرسة، 30 معلمًا، 12 صفًا، 8 مواد، 1000 طالب و5 أيام حضور. يتجاوز كل مدرسة مكتملة باسم `LoadTest School N`. تحفظ كل دفعة وتُعتمد مباشرة لتفريغ transaction log؛ الروابط والعلامات بدفعات 500 والحضور بدفعات 250. تبقى المدرسة `IsDeleted=true` أثناء البناء وتُفعّل بعد اكتمالها. إذا انقطع التنفيذ، يفحص السيدر المدرسة غير المكتملة ويضيف الروابط والعلامات والحضور المفقود فقط دون حذف أو تكرار السجلات الموجودة.

ملاحظة قيود: `StageClass.Code` و`Branch.BranchCode` بطول حرف واحد؛ يستخدم Seeder القيمة `L`.

## 12. الإعداد والتشغيل

المتطلبات:

- .NET SDK يدعم net9.0.
- SQL Server على connection string المضبوط.
- Redis على localhost:6379.

أوامر شائعة:

```powershell
dotnet restore
dotnet build
dotnet watch
dotnet ef migrations list
dotnet ef migrations has-pending-model-changes
dotnet ef database update
```

ينفذ التطبيق Migration تلقائيًا عند البدء في جميع البيئات، لذلك يجب أخذ نسخة احتياطية ومراجعة migrations قبل نشر إصدار Production. لا تستخدم `database drop --force` إلا لقاعدة اختبار وبعد التأكد من عدم وجود بيانات لازمة.

## 13. Migrations

تبدأ قاعدة البيانات بـ`InitialGuidIdentity` التي تنشئ المخطط الكامل بمفاتيح GUID. أضيفت `AddAttendanceQueryIndex` لتحسين استعلامات الحضور، و`ConvertAttendanceExcuseToNvarcharMax` لتحويل العذر من نوع SQL القديم `text` إلى `nvarchar(max)` القابل للبحث والترتيب دون تقليص البيانات. تضيف `AddDirectorates` طبقة المديريات، وتضيف `AddMinistriesTransfersAndAssignments` الوزارات والتبعيات وطلبات النقل وتصنيفات المدارس، بينما تضيف `BackfillOrganizationAssignments` رقم الهوية للطلبات وترحّل ارتباطات قواعد البيانات القائمة فقط. لا تحتوي هذه migrations على بيانات اختبار لقاعدة نظيفة. عند تعديل نموذج EF:

```powershell
dotnet ef migrations add DescriptiveName
dotnet ef database update
```

راجع migration قبل تطبيقها في Production وخذ نسخة احتياطية.

## 14. التسجيل ومعالجة الأخطاء

`ErrorHandlingMiddleware` يلتقط الاستثناءات، و`ErrorLoggerService` يخزن التفاصيل في `ErrorLog`. في Development تظهر صفحة المطور؛ في Production يستخدم `/Home/Error` وHSTS.

رسالة startup التي تقول `Database initialization failed` قد تشمل خطأ migration أو إنشاء الأدوار أو Seeder الأدمن، وفي Development قد تشمل LoadTest Seeder؛ راجع inner exception دائمًا.

## 15. التصدير والشهادات والصور

- EPPlus ينشئ ملفات Excel لقوائم الطلاب والمعلمين والعلامات.
- QuestPDF مع خطوط Amiri ينشئ شهادات عربية. شهادة المعلم تجمع كل مواده في صفحة واحدة وتستخدم «المادة/المواد» حسب العدد. شهادات الطالب والمعلم تحترم ملكية الملف ونطاق المدرسة.
- الصور الخاصة تحفظ خارج العرض static المباشر وتُخدم عبر Controller بعد التحقق من المستخدم واسم الملف.

## 16. الاختبارات والضغط

البناء الحالي يمر دون أخطاء. توجد تحذيرات nullable قديمة وتحذيرات أمنية لحزم MailKit 4.10.0 وImageSharp 3.1.8، ويجب جدولة تحديثهما واختبار التوافق.

لا توجد حاليًا حزمة اختبارات آلية واسعة. الحد الأدنى قبل النشر:

1. اختبار الدخول لكل دور.
2. محاولة معلم فتح GUID لمعلم وطالب غير تابعين له؛ المتوقع 403.
3. محاولة طالب فتح بيانات طالب آخر؛ المتوقع 403.
4. محاولة Manager فتح مدرسة أخرى؛ المتوقع 403.
5. اختبار CSRF للطلبات POST.
6. فحص HIT/MISS في Redis.
7. اختبار ضغط بأداة مثل k6 يتضمن زوارًا عامًا وتسجيل دخول حقيقيًا.

مقاييس الضغط المهمة: requests/sec، p95 وp99، نسبة 4xx/5xx، اتصالات SQL، CPU/RAM، Redis hit ratio، مدة login وlockout، وحجم connection pool.

## 17. إضافة ميزة أو Endpoint جديد

قائمة تحقق إلزامية:

1. استخدم GUID لكل PK/FK جديد.
2. أضف `[AuthorizeRoles(...)]` أو `[AllowAnonymous]` عن قصد.
3. صفِّ البيانات حسب المستخدم/المدرسة، ولا تعتمد على GUID وحده.
4. لا تعتمد على Session للصلاحيات.
5. استخدم ViewModel بدل bind مباشر لكيان واسع.
6. أضف CSRF للطلبات التي تغير الحالة.
7. أبطل مفاتيح Redis المتأثرة.
8. أضف index للاستعلامات المتكررة.
9. أضف migration واختبارات نجاح ومنع وصول.
10. لا تسجل كلمات مرور أو tokens أو connection strings.

## 18. استكشاف الأخطاء

- **AspNetRoles موجود مسبقًا:** قاعدة قديمة مع migration history غير متطابق؛ أعد إنشاء قاعدة الاختبار فقط بعد التأكد من البيانات.
- **Redis لا يظهر مفاتيح:** افتح شاشة المدير التي تحمل القوائم أولًا، ثم افحص endpoint؛ أول طلب MISS والثاني HIT.
- **403 بعد تحديث الحماية:** سجل الخروج والدخول لتجديد claim `active` والأدوار.
- **Seeder الأدمن لا يعمل:** تحقق من `SeedAdmin:Enabled` ومن إعدادات الحساب وكلمة المرور الآمنة. **LoadTest Seeder لا يعمل:** يجب أن تكون البيئة Development مع `LoadTestSeed:Enabled=true`.
- **خطأ truncation:** راجع MaxLength في DbContext، خصوصًا الرموز ذات الحرف الواحد.
- **Cookie لا تعمل على HTTP:** SecurePolicy=Always؛ استخدم عنوان HTTPS من launchSettings.

## 19. ملاحظات الإنتاج

- لا تعتمد على الإعداد وحده: الكود يمنع LoadTest Seeder خارج Development، ومع ذلك أبقِ `LoadTestSeed:Enabled=false` أو احذف قسمه من إعدادات Production.
- انقل connection strings وSMTP وRedis والأسرار إلى secret store.
- استخدم Redis connection من configuration بدل localhost الثابت.
- فعّل TLS وreverse proxy موثوقًا.
- حدّث الحزم ذات التنبيهات الأمنية.
- أضف rate limiting لتسجيل الدخول والاستعادة والـAPIs المكلفة.
- أضف health checks لـSQL وRedis.
- أضف اختبارات تكامل وسياسات backup/restore ومراقبة مركزية.

## 20. مرجع API

### 20.1 قواعد عامة

- العنوان المحلي في بيئة التطوير: `http://localhost:1908`. استخدم الأصل الفعلي للبيئة ولا تثبته في JavaScript خارج التطبيق.
- المصادقة Cookie عبر ASP.NET Core Identity؛ ترسل الواجهة `credentials: 'same-origin'`.
- طلبات `POST` و`PUT` و`DELETE` ترسل CSRF token في الترويسة `RequestVerificationToken`.
- كل معرف من نوع GUID بالصيغة `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`.
- DataTables يرسل `draw`, `start`, `length`, `search[value]`, `order[0][column]`, `order[0][dir]`.
- حد الصفحة الأقصى لنقاط DataTables الحديثة 100 سجل.

استجابة DataTables القياسية:

```json
{
  "draw": 1,
  "recordsTotal": 250,
  "recordsFiltered": 18,
  "data": []
}
```

استجابة نجاح الكتابة:

```json
{
  "success": true,
  "message": "تم الحفظ بنجاح.",
  "redirectUrl": "/Attendance/ViewAttendance?teacherId=..."
}
```

أكواد HTTP المتوقعة:

| الكود | المعنى |
|---|---|
| `200` | نجاح القراءة/التعديل/الحذف |
| `400` | نموذج أو GUID أو CSRF غير صالح |
| `401` | لا توجد مصادقة صالحة |
| `403` | الدور أو ملكية المورد لا تسمح |
| `404` | المورد غير موجود داخل النطاق المسموح |
| `409` | تعارض منطقي/تكرار عندما يعيده endpoint |
| `500` | خطأ داخلي مسجل في `ErrorLog` دون إرجاع stack trace |

### 20.2 Attendance API

| Method | المسار | الدور | الغرض والناتج |
|---|---|---|---|
| GET | `/api/Attendance/teacher-records?teacherId={guid}` | Teacher | DataTables لسجلات المعلم؛ الطالب، الصف، المادة، الحالة، التاريخ، العذر و`id`. |
| GET | `/api/Attendance/subjects?teacherId={guid}` | Teacher | `[{ id, name }]` لمواد المعلم الفعالة. |
| GET | `/api/Attendance/classes?teacherId={guid}&subjectId={guid}` | Teacher | `[{ id, name }]` لصفوف المعلم في المادة. |
| GET | `/api/Attendance/student-summary?studentid={guid}` | Admin/Manager/Student | DataTables مجمع حسب المادة+المعلم: `teacherId`, `teacherName`, `lectuerId`, `lectuerName`, `attendanceDays`, `totalDays`. |
| GET | `/api/Attendance/student-details?studentid={guid}&teacherId={guid}&lectuerId={guid}` | Admin/Manager/Student | DataTables للتفاصيل: `id`, `dateAndTime`, `attendanceStatus`, `excuse`. |
| GET | `/api/Attendance/student-records?studentid={guid}` | Admin/Manager/Student | القائمة المفصلة القديمة؛ محافظ عليها للتوافق، والواجهة الحالية تستخدم summary/details. |
| POST | `/api/Attendance/records` | Teacher | إنشاء أو تحديث حضور اليوم لدفعة طلاب؛ يعيد success/message/redirectUrl. |
| PUT | `/api/Attendance/records/{id}` | Teacher | تعديل `status` و`excuse` لسجل يملكه المعلم. |
| DELETE | `/api/Attendance/records/{id}` | Teacher | حذف سجل يملكه المعلم. |

طلب إنشاء الحضور:

```json
{
  "teacherId": "00000000-0000-0000-0000-000000000000",
  "lectuerId": "00000000-0000-0000-0000-000000000000",
  "classId": "00000000-0000-0000-0000-000000000000",
  "items": [
    { "studentId": "00000000-0000-0000-0000-000000000000", "status": "1", "excuse": null }
  ]
}
```

القيم المسموحة لـ`status`: `1` حضور، `0` غياب، `m` غياب بعذر. الحد الأقصى 500 طالب، وتمنع المعرفات المكررة. الخادم لا يثق بـ`teacherId`: يطابقه مع مستخدم Identity ويتحقق من تكليف المادة/الصف ومن ارتباط كل طالب بالمعلم.

### 20.3 Grades API

| Method | المسار | الدور | الغرض والناتج |
|---|---|---|---|
| GET | `/api/Grades/teacher-records?teacherId={guid}` | Teacher | DataTables لعلامات طلاب المعلم. |
| GET | `/api/Grades/student-records?studentid={guid}` | Admin/Manager/Student | DataTables لعلامات الطالب: المادة وأجزاء العلامة والمجموع. |
| GET | `/api/Grades/subjects?teacherId={guid}` | Teacher | `[{ id, name }]` للمواد الفعالة. |
| GET | `/api/Grades/classes?teacherId={guid}&subjectId={guid}` | Teacher | `[{ id, name }]` للصفوف الفعالة في المادة. |
| POST | `/api/Grades/records` | Teacher | Upsert جماعي للعلامات ويعيد success/message/redirectUrl. |
| PUT | `/api/Grades/records/{id}` | Teacher | تعديل أجزاء علامة يملكها المعلم. |
| DELETE | `/api/Grades/records/{id}` | Teacher | حذف سجل علامة يملكه المعلم. |

طلب حفظ العلامات:

```json
{
  "teacherId": "00000000-0000-0000-0000-000000000000",
  "lectuerId": "00000000-0000-0000-0000-000000000000",
  "classId": "00000000-0000-0000-0000-000000000000",
  "items": [
    {
      "studentId": "00000000-0000-0000-0000-000000000000",
      "firstMonth": 20,
      "mid": 30,
      "secondMonth": 20,
      "activity": 10,
      "final": 20
    }
  ]
}
```

كل جزء يقبل `null` أو قيمة من 0 إلى 100. القيم الفارغة تحفظ صفرًا. يتحقق الخادم من المدرسة والتكليف وكل طالب قبل تنفيذ دفعة واحدة.

### 20.4 Student dashboard API

| Method | المسار | الدور | الناتج |
|---|---|---|---|
| GET | `/api/student/grade-chart?idStudent={guid}` | Student | مصفوفة `{ lectuerName, totalGrade }` مجمعة حسب المادة؛ `totalGrade` متوسط المجموع عند تعدد السجلات. |
| GET | `/api/student/attendance-chart?idStudent={guid}` | Student | `{ subjectName, totalSessions, presentCount, excusedCount, presentPercentage, excusedPercentage }` لكل مادة. |
| GET | `/api/student/Details?id={guid}` | Admin/Manager | تفاصيل طالب ضمن نطاق المدرسة. |
| POST | `/api/student/Create` | Admin/Manager | إنشاء طالب وحسابه/ملفه وفق النموذج المستخدم في الواجهة؛ يتطلب CSRF. |
| GET | `/api/student/Edit?id={guid}` | Admin/Manager | بيانات نموذج التعديل ضمن المدرسة. |
| PUT | `/api/student/Edit` | Admin/Manager | تعديل الطالب؛ JSON وCSRF. |
| DELETE | `/api/student/Delete` | Admin/Manager | حذف منطقي/إداري حسب التنفيذ، مع نموذج `{ id }` وCSRF. |
| GET/POST | `/api/student/ChangeClass` | Admin/Manager | جلب بيانات تغيير الصف ثم تنفيذ التغيير ضمن المدرسة. |

رسوم الطالب لا تقبل طالبًا آخر حتى لو تغير GUID؛ `ValidateStudentDataAccessAsync` يطابق ملف الطالب مع مستخدم Identity.

### 20.5 Teacher, manager, class, and subject APIs

| المجموعة | المسارات الرئيسية | الوظيفة |
|---|---|---|
| Teacher | `/api/teacher/Create`, `/Details`, `/Edit`, `/Delete` | إنشاء/قراءة/تعديل/حذف المعلم ضمن الدور والمدرسة. |
| Teacher assignments | `/api/teacher/AddTeacherToClassesAndLectuers`, `/RemoveTeacherToClassLectuers`, `/ManagerStudentToTeacher` | إدارة إسنادات المعلم والطلاب. |
| Teacher charts | `/api/teacher/grade-distribution`, `/api/teacher/attendance-summary` | بيانات رسوم المعلم بعد فحص ملكيته. |
| Manager tables | `/api/menegar/MenegarStudent`, `/MenegarTeacher`, `/MenegarClass`, `/MenegarStudentInClass`, `/MenegarTeacherInClass` | DataTables مقيدة بمدرسة المدير. |
| Manager statistics | `/api/menegar/CountTeacherPerSubject` | عدد المعلمين حسب المادة ضمن المدرسة. |
| Classes | `/api/theClass/GetClasses`, `/GetClassToStudent`, `/Create`, `/Edit`, `/CreateTeacherClass`, `/Delete` | قوائم وإدارة الصفوف والإسناد. |
| Subjects | `/api/lectuer/GetLectuers`, `/LectuersData`, `/Create`, `/Edit`, `/TeacherLectuer`, `/StudentLectuer`, `/Delete`, `/DeleteTeacher` | قوائم وإدارة المواد وروابطها. |

توجد نقاط تاريخية في بعض Controllers تستعمل اسم الإجراء عند عدم تحديد route template. للاستهلاك الجديد، اعتمد فقط المسارات الصريحة الموضحة في هذا المرجع، وأضف template صريحًا لأي endpoint جديد.

### 20.6 التصدير والصور وRedis

| Method | المسار | الوصف |
|---|---|---|
| GET | `/api/ExportDataApi/...` | تصدير البيانات إلى Excel؛ اسم الإجراء/المعاملات يحددان نوع التصدير، ويتطلب الدور المناسب. |
| POST | endpoints رفع صورة الملف | `multipart/form-data`، CSRF، تحقق الامتداد والحجم واسم الملف. |
| GET | `/api/diagnostics/redis` | Admin فقط؛ حالة Redis وعدد المفاتيح ومعلومات type/TTL/size دون كشف المحتوى الحساس. |

الشهادات PDF ليست API JSON؛ هي MVC downloads محمية:

```text
GET /Teacher/DownloadTeacherCertificate?idTeacher={guid}
GET /Student/DownloadStudentCertificate?idStudent={guid}
```

شهادة المعلم تعرض جميع مواده دون تكرار في صفحة واحدة. شهادة الطالب والمعلم تتحققان من ملكية المستخدم أو نطاق مدرسة المدير؛ Admin يملك النطاق الإداري المصرح.

### 20.7 مثال JavaScript آمن

```javascript
const token = document.querySelector(
  'input[name="__RequestVerificationToken"]'
).value;

const response = await fetch('/api/Grades/records/RECORD_GUID', {
  method: 'PUT',
  credentials: 'same-origin',
  headers: {
    'Content-Type': 'application/json',
    'RequestVerificationToken': token
  },
  body: JSON.stringify({
    firstMonth: 20,
    mid: 30,
    secondMonth: 20,
    activity: 10,
    final: 20
  })
});

if (!response.ok) {
  const error = await response.json().catch(() => ({}));
  throw new Error(error.message ?? 'Request failed');
}
```

### 20.8 حدود الثقة والصيانة

- لا يمثل GUID تصريحًا؛ كل endpoint يعيد فحص الدور والمدرسة والملكية.
- لا تُقبل قيم `teacherId` أو `studentid` القادمة من العميل دون مطابقتها مع Identity.
- لا تعِد stack traces أو connection strings أو tokens إلى العميل.
- عند تعديل عقد API، عدّل View/JavaScript وهذا المرجع في التغيير نفسه.
- أضف اختبارات تكامل لحالات 200 و400 و401 و403 و404، واختبار مستخدم يحاول الوصول إلى مورد مستخدم آخر.

## 21. طبقة المديريات

- الكيان `Directorate` يمثل مديرية تضم عدة مدارس، وكل مدرسة مرتبطة بمديرية ارتباطًا إلزاميًا.
- الدور `DirectorateManager` مستقل عن مدير المدرسة `Manager`، ولكل مديرية ملف مسؤول واحد فقط بقيود Unique على المديرية وحساب Identity.
- مسؤول المديرية يستطيع إضافة مدارس مديريته وتعديلها وتفعيلها أو تعطيلها، ولا يستطيع تغيير `DirectorateId` أو نقل المدرسة إلى مديرية أخرى.
- تعطيل المدرسة يستخدم `School.IsActive` ولا يحذف المدرسة أو بياناتها، وتحقق الجلسة يرفض حسابات المدرسة المعطلة.
- تعرض لوحة المديرية إحصاءات مجمعة دون معلومات شخصية.
- في Development فقط، ينشئ LoadTest Seeder وزارتين وحسابات `directorate1`, `directorate2`, ... ويوزع مدارس الاختبار عليها بالتناوب. لا تدخل هذه البيانات في migrations.
- المواد المشتركة هي اللغة العربية والرياضيات واللغة الإنجليزية والتربية الإسلامية، وتبقى مرتبطة بالمدرسة حتى تصميم كتالوج الوزارة.

### 21.1 واجهات المديرية

| الطريقة والمسار | الغرض | الاستجابة |
|---|---|---|
| `GET /api/directorate/dashboard` | هوية المديرية والإحصاءات | كائن JSON مجمع |
| `GET /api/directorate/schools` | مدارس المديرية الحالية | مصفوفة مدارس |
| `GET /api/directorate/active-schools` | المدارس الفعالة التابعة للمديرية | مصفوفة مدارس فعالة مع المؤشرات الأساسية |
| `GET /api/directorate/managers` | مديرو مدارس المديرية | الاسم والمدرسة وبيانات الاتصال وتاريخ الانضمام وحالة الحساب |
| `GET /api/directorate/teachers` | معلمو مدارس المديرية | البيانات الأساسية والمدرسة وعدد المواد والصفوف وحالة الحساب |
| `GET /api/directorate/students` | طلاب مدارس المديرية | البيانات الأساسية والمدرسة والصف وتاريخ الانضمام وحالة الحساب |
| `GET /api/directorate/classes` | صفوف مدارس المديرية | المدرسة والمرحلة والرقم والشعبة والفرع وأعداد الطلاب والمعلمين |
| `GET /api/directorate/directory-options?schoolId={id}&personType={type}` | المدارس الفعالة وصفوف مدرسة مملوكة؛ وعند `manager` يستبعد المدارس التي لها مدير | خيارات نماذج الإضافة |
| `POST /api/directorate/managers` | إنشاء ملف مدير مدرسة ضمن المديرية | 201 ومعرف المدير |
| `POST /api/directorate/teachers` | إنشاء ملف معلم ضمن المديرية | 201 ومعرف المعلم |
| `POST /api/directorate/students` | إنشاء ملف طالب وربطه بتكليفات صفه الحالية | 201 ومعرف الطالب |
| `GET /api/directorate/schools/{id}` | قراءة مدرسة مملوكة | كائن مدرسة أو 403 |
| `GET /api/directorate/schools/{id}/report` | تقرير تشغيلي وتعليمي مجمع لمدرسة مملوكة | ملف المدرسة والمؤشرات والحضور والعلامات والصفوف والمواد أو 403 |
| `GET /api/directorate/school-options` | خيارات الحالة والنوع والمرحلة | كائن خيارات |
| `POST /api/directorate/schools` | إنشاء مدرسة | 201 ومعرف المدرسة |
| `PUT /api/directorate/schools/{id}` | تعديل دون نقل | 204 |
| `PATCH /api/directorate/schools/{id}/activation` | تفعيل أو تعطيل دون حذف | 204 |

طلبات الكتابة محمية بـIdentity والدور وملكية المديرية وCSRF، ولا يُوثق بمعرف مديرية قادم من المتصفح. مسارات القراءة تعيد كذلك فحص ملكية المدرسة؛ وجود GUID صحيح لا يمنح صلاحية الوصول.

### 21.2 واجهات MVC وتجربة المستخدم

| المسار | الوظيفة |
|---|---|
| `GET /Directorate` | لوحة المديرية: بطاقات إحصائية تفاعلية مرتبطة بصفحات التفاصيل، مع حركة Hover ودعم لوحة المفاتيح. |
| `GET /Directorate/Schools` | جدول مدارس المديرية مع البحث، اختيار حجم الصفحة، ترتيب النتائج، Pagination، والتعديل والتفعيل والتفاصيل. |
| `GET /Directorate/ActiveSchools` | قائمة المدارس الفعالة ومؤشراتها وروابط تقاريرها. |
| `GET /Directorate/Managers` | جدول مديري المدارس وحالة الحساب. |
| `GET /Directorate/Teachers` | جدول المعلمين وتغطية المواد والصفوف. |
| `GET /Directorate/Students` | جدول الطلاب والمدارس والصفوف وحالة الحساب. |
| `GET /Directorate/Classes` | جدول توزيع الصفوف والطلاب والمعلمين على المدارس. |
| `GET /Directorate/CreateManager` | نموذج ملف مدير مدرسة كامل ضمن مدرسة مملوكة. |
| `GET /Directorate/CreateTeacher` | نموذج ملف معلم كامل ضمن مدرسة مملوكة. |
| `GET /Directorate/CreateStudent` | نموذج ملف طالب كامل مع اختيار مدرسة وصف تابع لها. |
| `GET /Directorate/CreateSchool` | نموذج إنشاء مدرسة. |
| `GET /Directorate/EditSchool/{id}` | نموذج تعديل مدرسة مملوكة للمديرية. |
| `GET /Directorate/SchoolDetails/{id}` | تقرير المدرسة القابل للطباعة. |

جدول المدارس مبني على DataTables في المتصفح ويستهلك `GET /api/directorate/schools`. يعرض 10 سجلات افتراضيًا مع خيارات 5 و10 و25 و50، ويحافظ على صفحة المستخدم بعد تغيير حالة مدرسة. فُعّل `scrollX`، وأضيفت حاوية `overflow-x: auto` وحد أدنى لعرض الجدول حتى لا تنضغط الأعمدة أو أزرار الإجراءات على الشاشات الصغيرة.

إنشاء المدير أو المعلم أو الطالب من طبقة المديرية ينشئ ملفه التشغيلي الكامل بعد التحقق من المدرسة والصف والعمر وتفرّد الهوية والبريد. إنشاء اسم المستخدم وكلمة المرور وربط دور Identity يبقى عبر تدفق `Account/SetCredentials` الحالي باستخدام رقم الهوية، ولا تخزن نماذج المديرية كلمات مرور.

### 21.3 عقد تقرير المدرسة

يستهلك `SchoolDetails.cshtml` endpoint واحدًا:

```text
GET /api/directorate/schools/{schoolId}/report
```

الاستجابة JSON وتتكون من:

- `school`: الاسم، التفعيل، التصنيف الرسمي، النوع، المرحلة، وأدنى/أعلى صف.
- `summary`: أعداد المديرين والمعلمين والطلاب والصفوف والمواد الفعالة.
- `attendance`: إجمالي السجلات، الحضور (`1`)، الغياب (`0`)، الغياب بعذر (`m`)، و`attendanceRate`. تحسب نسبة الالتزام من `(الحضور + الغياب بعذر) / الإجمالي`، وتكون `null` عند عدم وجود سجلات.
- `academic`: عدد سجلات العلامات ذات `Total`، المتوسط، و`distribution` ضمن الفئات: أقل من 50، 50-59، 60-69، 70-79، 80-89، و90-100. كل فئة تعيد `label`, `count`, `percentage`.
- `classes`: اسم الصف، المرحلة، الرقم، الشعبة، الفرع، وعدد الطلاب غير المحذوفين.
- `subjects`: اسم المادة وعدد المعلمين والصفوف المرتبطة فعليًا. تحسب الأعداد من تكليفات `TeacherLectuerClass` غير المحذوفة وبقيم معرفات صالحة.
- `generatedAt`: وقت إنشاء التقرير بصيغة UTC.

لا يعرض التقرير أسماء الطلاب أو المعلمين أو أرقام الهوية أو بيانات الاتصال. المقصود منه المراقبة المجمعة على مستوى المديرية، وتطبق عليه `ValidateDirectorateSchoolAccessAsync` في مسار MVC ومسار API.

واجهة التقرير توفر حالة تحميل، حالة خطأ، حالات فارغة، جداول بتمرير أفقي، وزر طباعة مرتبط بحدث JavaScript يستدعي `window.print()`. أنماط الطباعة تخفي عناصر التنقل والأزرار وتزيل الظلال غير الضرورية.

### 21.4 قابلية إعادة الاستخدام

طبقة العرض مبنية كعميل لعقود `/api/directorate/*`، لذلك يمكن إعادة استخدام العقد في تطبيق ويب أو جوال آخر. إعادة الاستخدام لا تعني أن endpoint عام: يجب على العميل الجديد إرسال سياق المصادقة المعتمد وتكييف Identity/Session وCSRF حسب بيئته، مع الحفاظ على فحص الدور والملكية في الخادم. عند تغيير أسماء الحقول أو دلالات المؤشرات، يجب تحديث الواجهة وهذا القسم واختبارات التكامل معًا.
## 22. طبقة الوزارة

تمثل صلاحية `Admin` طبقة الوزارة حاليًا للمحافظة على توافق الحسابات وقواعد الصلاحيات الموجودة. الطبقة إشرافية على مستوى النظام ولا تمنح الوزارة مسارات التشغيل اليومي الخاصة بمدير المديرية أو المدرسة.

### 22.1 واجهات الوزارة

| المسار | الوظيفة |
|---|---|
| `GET /Ministry` | لوحة مؤشرات وطنية مجمعة. |
| `GET /Ministry/Directorates` | دليل المديريات مع البحث والترتيب وحجم الصفحة وPagination. |
| `GET /Ministry/DirectorateDetails/{id}` | ملف مديرية، مديرها، مؤشراتها، والمدارس التابعة لها. |

تستخدم الصفحات اتجاه RTL ومكونات المشروع المشتركة وNotyf للإشعارات. جدول المديريات يدعم التمرير الأفقي عند الحاجة فقط، وتظهر البطاقات والروابط بحالات Hover وFocus واضحة.

### 22.2 عقود API

| الطريقة والمسار | الغرض |
|---|---|
| `GET /api/ministry/dashboard` | الإحصاءات المجمعة للمديريات والمدارس والمديرين والمعلمين والطلاب والصفوف. |
| `GET /api/ministry/directorates` | دليل المديريات ومدير كل مديرية ومؤشراتها. |
| `GET /api/ministry/directorates/{id}` | التقرير التفصيلي لمديرية محددة والمدارس التابعة لها. |
| `PATCH /api/ministry/directorates/{id}/activation` | تفعيل أو تعطيل المديرية دون حذفها. |

جميع مسارات MVC وAPI محمية بدور `Admin`. تعطيل المديرية لا يحذف بياناتها، كما يمنع تحقق جلسة مدير المديرية دخوله عندما تصبح `Directorate.IsActive` غير فعالة. ولا تعيد صفحات الوزارة استخدام تقرير المدرسة المحمي بدور مدير المديرية؛ سيضاف تقرير مدرسة خاص بالوزارة عند تنفيذ مرحلته مع إبقاء حدود التفويض مستقلة.
## 23. الهيكل التنظيمي وطلبات النقل

أصبح التسلسل التنظيمي `Ministry -> Directorate -> School`. تحتوي قاعدة التطوير المحلية الحالية وزارتين (`MIN-01`, `MIN-02`) ومديريتين (`LOAD-DIR-01`, `LOAD-DIR-02`)، وحذفت مديرية `LEGACY` فقط بعد التحقق من خلوها. هذه بيانات تطوير وليست جزءًا من migrations أو بيانات الإنتاج. تتبع المدارس 1 و3 المديرية الأولى، والمدرسة 2 المديرية الثانية، وحُفظت مدرسة «يبنا أ للبنين» ضمن المديرية الأولى.

أضيفت الجداول `TeacherPlacement` لدعم عدة مدارس للمعلم مع تبعية أساسية واحدة، و`SchoolManagerAssignment` لدعم إدارة عدة مدارس، و`StudentEnrollment` مع فهرس يفرض تسجيلًا فعالًا واحدًا للطالب. رحّل migration `BackfillOrganizationAssignments` الارتباطات القائمة دون حذف الأشخاص: 90 تبعية معلم، 7 تكليفات مدير مدرسة، و3001 تسجيل طالب.

طلبات النقل تستخدم رقم الهوية مع نوع الشخص (`Teacher`, `Student`, `SchoolManager`). النقل داخل المديرية ينفذ مباشرة، أما النقل بين مديريتين فينشئ طلبًا معلقًا لدى المديرية المصدر. الموافقة تغلق التبعية الأساسية السابقة، تنشئ التبعية الجديدة، تحدّث حقول التوافق القديمة، وتعيد تفعيل الحساب عند الحاجة.

| المسار | الغرض |
|---|---|
| `GET /Transfers` | صفحة إنشاء ومتابعة طلبات النقل. |
| `GET /api/transfers/options` | المدارس والصفوف المسموح بها للجهة الحالية. |
| `GET /api/transfers?direction=incoming|outgoing` | الطلبات الواردة أو الصادرة. |
| `POST /api/transfers` | إنشاء طلب برقم الهوية والمدرسة المستقبلة. |
| `PATCH /api/transfers/{id}/decision` | الموافقة أو الرفض وتنفيذ النقل عند الموافقة. |
| `GET /api/ministry/ministries` | الوزارات ومديرياتها ومدارسها ومؤشرات الأشخاص. |

إعادة إضافة معلم معطل من واجهة المديرية تعيد تفعيل السجل والحساب نفسيهما وتنشئ تبعية مدرسية أساسية جديدة بدل إنشاء هوية مكررة. كما أصبحت عمليات إنشاء المدير والمعلم والطالب تكتب في جداول التبعية الجديدة والحقول القديمة معًا خلال فترة الانتقال.
## 24. سياسة بيانات الإنتاج والبذر

المهاجرات بنيوية فقط ولا تضيف وزارات أو مديريات أو مدارس أو أشخاصًا أو طلبات نقل تجريبية. تم التحقق بتطبيق سلسلة migrations كاملة على قاعدة LocalDB نظيفة، وكانت أعداد `Ministry`, `Directorate`, `School`, `Teacher`, `Student`, `Menegar`, و`TransferRequest` جميعها صفرًا.

`LoadTestDataSeeder` محصور بشرطين معًا: أن تكون البيئة `Development` وأن تكون `LoadTestSeed:Enabled=true`. لذلك لا يمكن تشغيله في Production حتى لو أضيف الإعداد بالخطأ. أما `IdentityDataSeeder.SeedMainAdminAsync` فهو Seeder الإنتاج الوحيد؛ يعمل عندما يكون `SeedAdmin:Enabled=true` وتُمرر بيانات الحساب الرسمي وكلمة المرور من إعدادات نشر آمنة أو متغيرات البيئة/مخزن الأسرار. لا تحفظ كلمة مرور الأدمن داخل المستودع.

عند بدء التطبيق تطبق migrations، تضمن الأدوار الأساسية، ثم تنشئ أو تحدّث حساب الأدمن الرسمي فقط. سجل `LEGACY` في migration القديم مشروط بوجود مدارس قبل الترقية لحماية قواعد قديمة؛ ولا يُنشأ في قاعدة إنتاج نظيفة.
