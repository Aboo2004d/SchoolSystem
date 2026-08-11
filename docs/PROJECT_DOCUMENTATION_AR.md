# توثيق نظام إدارة المدارس

> هذا الملف يصف الحالة الفعلية للمشروع بعد تحويل المصادقة إلى ASP.NET Core Identity، وتحويل معرفات المجال إلى GUID، وإضافة فصل الأدوار والملكية وRedis وبيانات اختبار الضغط.

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
| `Controllers/` | صفحات MVC والعمليات التقليدية |
| `Controllers/ApiController/` | نقاط AJAX وJSON والتصدير والتشخيص |
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
6. في Development ينفذ `MigrateAsync`.
7. ينشئ الأدوار الناقصة.
8. يشغل Seeder الأدمن الرئيسي.
9. يشغل Seeder بيانات الضغط إذا كان مفعّلًا.
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
- `StudentController`: شاشة الطالب، تغيير الصف الإداري وشهادته.
- `TheClassController`: الصفوف وإسناد المعلمين.
- `LectuerController`: المواد وربط المعلمين والطلاب.
- `GradesController`: إنشاء وعرض وتعديل وحذف العلامات.
- `AttendanceController`: إنشاء وعرض وتعديل وحذف الحضور.
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

`LoadTestDataSeeder` يعمل فقط عند `LoadTestSeed:Enabled=true` وفي مسار تشغيل Development الحالي. ينشئ بيانات مترابطة بحفظ مرحلي و`AddRange`:

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

في Development ينفذ التطبيق Migration تلقائيًا. لا تستخدم `database drop --force` إلا لقاعدة اختبار وبعد التأكد من عدم وجود بيانات لازمة.

## 13. Migrations

Migration الحالية `InitialGuidIdentity` تنشئ المخطط كاملًا بمفاتيح GUID. يوجد أيضًا `InitialGuidIdentity.sql` للتطبيق اليدوي. عند تعديل نموذج EF:

```powershell
dotnet ef migrations add DescriptiveName
dotnet ef database update
```

راجع migration قبل تطبيقها في Production وخذ نسخة احتياطية.

## 14. التسجيل ومعالجة الأخطاء

`ErrorHandlingMiddleware` يلتقط الاستثناءات، و`ErrorLoggerService` يخزن التفاصيل في `ErrorLog`. في Development تظهر صفحة المطور؛ في Production يستخدم `/Home/Error` وHSTS.

رسالة startup التي تقول "Database migration failed" قد تشمل خطأ Seeder أيضًا لأن Migration والSeeders داخل كتلة try واحدة؛ راجع inner exception دائمًا.

## 15. التصدير والشهادات والصور

- EPPlus ينشئ ملفات Excel لقوائم الطلاب والمعلمين والعلامات.
- QuestPDF مع خطوط Amiri ينشئ شهادات عربية للطلاب والمعلمين.
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
- **Seeder لا يعمل:** تحقق من Enabled وكلمة المرور ووجود `LoadTest School` سابقًا.
- **خطأ truncation:** راجع MaxLength في DbContext، خصوصًا الرموز ذات الحرف الواحد.
- **Cookie لا تعمل على HTTP:** SecurePolicy=Always؛ استخدم عنوان HTTPS من launchSettings.

## 19. ملاحظات الإنتاج

- عطّل LoadTestSeed قطعًا.
- انقل connection strings وSMTP وRedis والأسرار إلى secret store.
- استخدم Redis connection من configuration بدل localhost الثابت.
- فعّل TLS وreverse proxy موثوقًا.
- حدّث الحزم ذات التنبيهات الأمنية.
- أضف rate limiting لتسجيل الدخول والاستعادة والـAPIs المكلفة.
- أضف health checks لـSQL وRedis.
- أضف اختبارات تكامل وسياسات backup/restore ومراقبة مركزية.
