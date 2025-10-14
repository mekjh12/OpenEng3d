using OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Ui3d
{
    public struct CharInfo
    {
        public char character;

        // 아틀라스 UV 좌표 (정규화된 0~1 범위) - 실제 글자 영역만
        public float uvX;
        public float uvY;
        public float uvWidth;
        public float uvHeight;

        // 월드 공간에서의 글자 크기
        public float width;
        public float height;

        // 다음 글자까지의 전진 거리 (자간 포함)
        public float advance;
    }

    /// <summary>
    /// 영문자, 숫자, 한글을 위한 텍스처 아틀라스 (타이트 패킹 방식)
    /// </summary>
    public class CharacterTextureAtlas : IDisposable
    {
        private static CharacterTextureAtlas _instance;
        public static CharacterTextureAtlas Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("CharacterTextureAtlas not initialized. Call Initialize() first.");
                return _instance;
            }
        }

        // ✅ 최적화된 설정 (메모리 67MB, 초기화 2~3초)
        private const int ATLAS_WIDTH = 8192;
        private const int ATLAS_HEIGHT = 3072;
        private const float PADDING = 3f;
        private const float ATLAS_FONT_SIZE = 48f;
        private const float WORLD_SCALE = 0.0012f;

        // ✅ 한글 완성형 2,350자 (KS X 1001)
        private const string HANGUL_2350 =
            "가각간갇갈갉갊감갑값갓갔강갖갗같갚갛개객갠갤갬갭갯갰갱갸갹갼걀걋걍걔걘걜거걱건걷걸걺검겁것겄겅겆겉겊겋게겐겔겜겝겟겠겡겨격겪견겯결겸겹겻겼경곁계곈곌곕곗고곡곤곧골곪곬곯곰곱곳공곶과곽관괄괆괌괍괏광괘괜괠괩괬괭괴괵괸괼굄굅굇굉교굔굘굡굣구국군굳굴굵굶굻굼굽굿궁궂궈궉권궐궜궝궤궷귀귁귄귈귐귑귓규균귤그극근귿글긁금급긋긍긔기긱긴긷길긺김깁깃깅깆깊까깍깎깐깔깖깜깝깟깠깡깥깨깩깬깰깸" +
            "깹깻깼깽꺄꺅꺌꺼꺽꺾껀껄껌껍껏껐껑께껙껜껨껫껭껴껸껼꼇꼈꼍꼐꼬꼭꼰꼲꼴꼼꼽꼿꽁꽂꽃꽈꽉꽐꽜꽝꽤꽥꽹꾀꾄꾈꾐꾑꾕꾜꾸꾹꾼꿀꿇꿈꿉꿋꿍꿎꿔꿜꿨꿩꿰꿱꿴꿸뀀뀁뀄뀌뀐뀔뀜뀝뀨끄끅끈끊끌끎끓끔끕끗끙끝끼끽낀낄낌낍낏낑나낙낚난낟날낡낢남납낫났낭낮낯낱낳내낵낸낼냄냅냇냈냉냐냑냔냘냠냥너넉넋넌널넒넓넘넙넛넜넝넣네넥넨넬넴넵넷넸넹녀녁년녈념녑녔녕녘녜녠노녹논놀놂놈놉놋농높놓놔놘놜놨뇌뇐뇔뇜뇝" +
            "뇟뇨뇩뇬뇰뇹뇻뇽누눅눈눋눌눔눕눗눙눠눴눼뉘뉜뉠뉨뉩뉴뉵뉼늄늅늉느늑는늘늙늚늠늡늣능늦늪늬늰늴니닉닌닐닒님닙닛닝닢다닥닦단닫달닭닮닯닳담답닷닸당닺닻닿대댁댄댈댐댑댓댔댕댜더덕덖던덛덜덞덟덤덥덧덩덫덮데덱덴델뎀뎁뎃뎄뎅뎌뎐뎔뎠뎡뎨뎬도독돈돋돌돎돐돔돕돗동돛돝돠돤돨돼됐되된될됨됩됫됴두둑둔둘둠둡둣둥둬뒀뒈뒝뒤뒨뒬뒵뒷뒹듀듄듈듐듕드득든듣들듦듬듭듯등듸디딕딘딛딜딤딥딧딨딩딪따딱딴딸" +
            "땀땁땃땄땅땋때땍땐땔땜땝땟땠땡떠떡떤떨떪떫떰떱떳떴떵떻떼떽뗀뗄뗌뗍뗏뗐뗑뗘뗬또똑똔똘똥똬똴뙈뙤뙨뚜뚝뚠뚤뚫뚬뚱뛔뛰뛴뛸뜀뜁뜅뜨뜩뜬뜯뜰뜸뜹뜻띄띈띌띔띕띠띤띨띰띱띳띵라락란랄람랍랏랐랑랒랖랗래랙랜랠램랩랫랬랭랴략랸럇량러럭런럴럼럽럿렀렁렇레렉렌렐렘렙렛렝려력련렬렴렵렷렸령례롄롑롓로록론롤롬롭롯롱롸롼뢍뢨뢰뢴뢸룀룁룃룅료룐룔룝룟룡루룩룬룰룸룹룻룽뤄뤘뤠뤼뤽륀륄륌륏륑류륙륜률륨륩" +
            "륫륭르륵른를름릅릇릉릊릍릎리릭린릴림립릿링마막만많맏말맑맒맘맙맛망맞맡맣매맥맨맬맴맵맷맸맹맺먀먁먈먕머먹먼멀멂멈멉멋멍멎멓메멕멘멜멤멥멧멨멩며멱면멸몃몄명몇몌모목몫몬몰몲몸몹못몽뫄뫈뫘뫙뫼묀묄묍묏묑묘묜묠묩묫무묵묶문묻물묽묾뭄뭅뭇뭉뭍뭏뭐뭔뭘뭡뭣뭬뮈뮌뮐뮤뮨뮬뮴뮷므믄믈믐믓미믹민믿밀밂밈밉밋밌밍및밑바박밖밗반받발밝밞밟밤밥밧방밭배백밴밸뱀뱁뱃뱄뱅뱉뱌뱍뱐뱝버벅번벋벌벎범법벗" +
            "벙벚베벡벤벧벨벰벱벳벴벵벼벽변별볍볏볐병볕볘볜보복볶본볼봄봅봇봉봐봔봤봬뵀뵈뵉뵌뵐뵘뵙뵤뵨부북분붇불붉붊붐붑붓붕붙붚붜붤붰붸뷔뷕뷘뷜뷩뷰뷴뷸븀븃븅브븍븐블븜븝븟비빅빈빌빎빔빕빗빙빚빛빠빡빤빨빪빰빱빳빴빵빻빼빽뺀뺄뺌뺍뺏뺐뺑뺘뺙뺨뻐뻑뻔뻗뻘뻠뻣뻤뻥뻬뼁뼈뼉뼘뼙뼛뼜뼝뽀뽁뽄뽈뽐뽑뽕뾔뾰뿅뿌뿍뿐뿔뿜뿟뿡쀼쁑쁘쁜쁠쁨쁩삐삑삔삘삠삡삣삥사삭삯산삳살삵삶삼삽삿샀상샅새색샌샐샘샙샛샜생샤" +
            "샥샨샬샴샵샷샹섀섄섈섐섕서석섞섟선섣설섦섧섬섭섯섰성섶세섹센셀셈셉셋셌셍셔셕션셜셤셥셧셨셩셰셴셸솅소속솎손솔솖솜솝솟송솥솨솩솬솰솽쇄쇈쇌쇔쇗쇘쇠쇤쇨쇰쇱쇳쇼쇽숀숄숌숍숏숑수숙순숟술숨숩숫숭숯숱숲숴쉈쉐쉑쉔쉘쉠쉥쉬쉭쉰쉴쉼쉽쉿슁슈슉슐슘슛슝스슥슨슬슭슴습슷승시식신싣실싫심십싯싱싶싸싹싻싼쌀쌈쌉쌌쌍쌓쌔쌕쌘쌜쌤쌥쌨쌩썅써썩썬썰썲썸썹썼썽쎄쎈쎌쏀쏘쏙쏜쏟쏠쏢쏨쏩쏭쏴쏵쏸쐈쐐쐤쐬쐰" +
            "쐴쐼쐽쑈쑤쑥쑨쑬쑴쑵쑹쒀쒔쒜쒸쒼쓩쓰쓱쓴쓸쓺쓿씀씁씌씐씔씜씨씩씬씰씸씹씻씽아악안앉않알앍앎앓암압앗았앙앝앞애액앤앨앰앱앳앴앵야약얀얄얇얌얍얏양얕얗얘얜얠얩어억언얹얻얼얽얾엄업없엇었엉엊엌엎에엑엔엘엠엡엣엥여역엮연열엶엷염엽엾엿였영옅옆옇예옌옐옘옙옛옜오옥온올옭옮옰옳옴옵옷옹옻와왁완왈왐왑왓왔왕왜왝왠왬왯왱외왹왼욀욈욉욋욍요욕욘욜욤욥욧용우욱운울욹욺움웁웃웅워웍원월웜웝웠웡웨" +
            "웩웬웰웸웹웽위윅윈윌윔윕윗윙유육윤율윰윱윳융윷으윽은을읊음읍읏응읒읓읔읕읖읗의읜읠읨읫이익인일읽읾잃임입잇있잉잊잎자작잔잖잗잘잚잠잡잣잤장잦재잭잰잴잼잽잿쟀쟁쟈쟉쟌쟎쟐쟘쟝쟤쟨쟬저적전절젊점접젓정젖제젝젠젤젬젭젯젱져젼졀졈졉졌졍졔조족존졸졺좀좁좃종좆좇좋좌좍좔좝좟좡좨좼좽죄죈죌죔죕죗죙죠죡죤죵주죽준줄줅줆줌줍줏중줘줬줴쥐쥑쥔쥘쥠쥡쥣쥬쥰쥴쥼즈즉즌즐즘즙즛증지직진짇질짊짐집짓" +
            "징짖짙짚짜짝짠짢짤짧짬짭짯짰짱째짹짼쨀쨈쨉쨋쨌쨍쨔쨘쨩쩌쩍쩐쩔쩜쩝쩟쩠쩡쩨쩽쪄쪘쪼쪽쫀쫄쫌쫍쫏쫑쫓쫘쫙쫠쫬쫴쬈쬐쬔쬘쬠쬡쭁쭈쭉쭌쭐쭘쭙쭝쭤쭸쭹쮜쮸쯔쯤쯧쯩찌찍찐찔찜찝찡찢찧차착찬찮찰참찹찻찼창찾채책챈챌챔챕챗챘챙챠챤챦챨챰챵처척천철첨첩첫첬청체첵첸첼쳄쳅쳇쳉쳐쳔쳤쳬쳰촁초촉촌촐촘촙촛총촤촨촬촹최쵠쵤쵬쵭쵯쵱쵸춈추축춘출춤춥춧충춰췄췌췐취췬췰췸췽츄츈츌츔츙츠측츤츨츰츱츳층" +
            "치칙친칟칠칡침칩칫칭카칵칸칼캄캅캇캉캐캑캔캘캠캡캣캤캥캬캭컁커컥컨컫컬컴컵컷컸컹케켁켄켈켐켑켓켕켜켠켤켬켭켯켰켱켸코콕콘콜콤콥콧콩콰콱콴콸쾀쾅쾌쾡쾨쾰쿄쿠쿡쿤쿨쿰쿱쿳쿵쿼퀀퀄퀑퀘퀭퀴퀵퀸퀼큄큅큇큉큐큔큘큠크큭큰클큼큽킁키킥킨킬킴킵킷킹타탁탄탈탉탐탑탓탔탕태택탠탤탬탭탯탰탱탸턍터턱턴털턺텀텁텃텄텅테텍텐텔템텝텟텡텨텬텼톄톈토톡톤톨톰톱톳통톺톼퇀퇘퇴퇸툇툉툐투툭툰툴툼툽툿퉁퉈퉜" +
            "퉤튀튁튄튈튐튑튕튜튠튤튬튱트특튼튿틀틂틈틉틋틔틘틜틤틥티틱틴틸팀팁팃팅파팍팎판팔팖팜팝팟팠팡팥패팩팬팰팸팹팻팼팽퍄퍅퍼퍽펀펄펌펍펏펐펑페펙펜펠펨펩펫펭펴편펼폄폅폈평폐폘폡폣포폭폰폴폼폽폿퐁퐈퐝푀푄표푠푤푭푯푸푹푼푿풀풂품풉풋풍풔풩퓌퓐퓔퓜퓟퓨퓬퓰퓸퓻퓽프픈플픔픕픗피픽핀필핌핍핏핑하학한할핥함합핫항해핵핸핼햄햅햇했행햐향허헉헌헐헒험헙헛헝헤헥헨헬헴헵헷헹혀혁현혈혐협혓혔형혜혠" +
            "혤혭호혹혼홀홅홈홉홋홍홑화확환활홧황홰홱홴횃횅회획횐횔횝횟횡효횬횰횹횻후훅훈훌훑훔훗훙훠훤훨훰훵훼훽휀휄휑휘휙휜휠휨휩휫휭휴휵휸휼흄흇흉흐흑흔흖흗흘흙흠흡흣흥흩희흰흴흼흽힁히힉힌힐힘힙힛힝";

        // ✅ 문자 세트 (영문 + 숫자 + 특수문자 + 한글)
        private const string CHARACTER_SET =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()-_=+[{]}\\|;:'\",.<>/? " +
            HANGUL_2350;

        // 폰트 설정
        private Font _font;
        private uint _textureId;
        private Dictionary<char, CharInfo> _charInfoMap;
        private bool _isInitialized = false;

        private CharacterTextureAtlas()
        {
            _charInfoMap = new Dictionary<char, CharInfo>();
        }

        public static void Initialize()
        {
            if (_instance != null)
            {
                Console.WriteLine("Warning: CharacterTextureAtlas already initialized");
                return;
            }

            _instance = new CharacterTextureAtlas();

            Console.WriteLine($"Initializing CharacterTextureAtlas with {CHARACTER_SET.Length} characters...");
            var startTime = DateTime.Now;

            _instance.CreateAtlas();

            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"CharacterTextureAtlas initialized successfully in {elapsed:F2} seconds");
        }

        public static bool IsInitialized => _instance != null && _instance._isInitialized;
        public uint TextureId => _textureId;

        public CharInfo GetCharInfo(char c)
        {
            if (_charInfoMap.TryGetValue(c, out CharInfo info))
                return info;

            Console.WriteLine($"Warning: Character '{c}' (U+{((int)c):X4}) not found in atlas, using default");
            return _charInfoMap.TryGetValue('A', out CharInfo defaultInfo) ? defaultInfo : new CharInfo();
        }

        public float CalculateTextWidth(string text)
        {
            float width = 0f;
            foreach (char c in text)
            {
                if (_charInfoMap.TryGetValue(c, out CharInfo info))
                    width += info.advance;
            }
            return width;
        }

        /// <summary>
        /// 타이트 패킹 방식으로 아틀라스 생성
        /// </summary>
        private void CreateAtlas()
        {
            _font = new Font(FontManager.DefaultFontFamily, ATLAS_FONT_SIZE, FontStyle.Regular, GraphicsUnit.Point);

            if (_font == null)
            {
                Console.WriteLine("Error: Failed to create font");
                return;
            }

            Console.WriteLine($"Atlas font size: {_font.Size}pt, Font family: {_font.FontFamily.Name}");

            using (Bitmap atlasBitmap = new Bitmap(ATLAS_WIDTH, ATLAS_HEIGHT))
            using (Graphics g = Graphics.FromImage(atlasBitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);

                // StringFormat 설정
                StringFormat format = StringFormat.GenericTypographic;
                format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                // 폰트 메트릭 계산
                float fontHeight = _font.GetHeight(g);
                float ascent = fontHeight * _font.FontFamily.GetCellAscent(_font.Style) /
                               _font.FontFamily.GetEmHeight(_font.Style);
                float descent = fontHeight * _font.FontFamily.GetCellDescent(_font.Style) /
                                _font.FontFamily.GetEmHeight(_font.Style);

                float commonHeight = ascent + descent;
                float rowHeight = commonHeight + PADDING * 4;

                Console.WriteLine($"Font metrics - Height: {fontHeight:F2}, Ascent: {ascent:F2}, Descent: {descent:F2}, Row height: {rowHeight:F2}");

                // 모든 글자의 크기와 advance를 측정
                Dictionary<char, SizeF> charSizes = new Dictionary<char, SizeF>();
                Dictionary<char, float> charAdvances = new Dictionary<char, float>();

                int processedCount = 0;
                foreach (char c in CHARACTER_SET)
                {
                    SizeF size = g.MeasureString(c.ToString(), _font, new PointF(0, 0), format);

                    string doubleChar = c.ToString() + c.ToString();
                    SizeF doubleSize = g.MeasureString(doubleChar, _font, new PointF(0, 0), format);
                    float advance = doubleSize.Width - size.Width;

                    charSizes[c] = size;
                    charAdvances[c] = advance;

                    processedCount++;
                    if (processedCount % 500 == 0)
                    {
                        Console.WriteLine($"Measured {processedCount}/{CHARACTER_SET.Length} characters...");
                    }
                }

                // 글자를 한 줄씩 배치
                float currentX = PADDING;
                float currentY = PADDING;
                int placedCount = 0;

                foreach (char c in CHARACTER_SET)
                {
                    SizeF charSize = charSizes[c];
                    float charWidth = charSize.Width + PADDING * 4;

                    // 현재 줄에 공간이 없으면 다음 줄로
                    if (currentX + charWidth > ATLAS_WIDTH)
                    {
                        currentX = PADDING;
                        currentY += rowHeight;

                        if (currentY + rowHeight > ATLAS_HEIGHT)
                        {
                            Console.WriteLine($"Warning: Atlas size too small! Only {placedCount}/{CHARACTER_SET.Length} characters fit.");
                            break;
                        }
                    }

                    float drawX = currentX + PADDING * 2;
                    float renderTop = currentY + PADDING * 2;

                    using (SolidBrush brush = new SolidBrush(Color.White))
                    {
                        g.DrawString(c.ToString(), _font, brush, drawX, renderTop, format);
                    }

                    CharInfo info = new CharInfo
                    {
                        character = c,
                        uvX = drawX / ATLAS_WIDTH,
                        uvY = renderTop / ATLAS_HEIGHT,
                        uvWidth = charSize.Width / ATLAS_WIDTH,
                        uvHeight = commonHeight / ATLAS_HEIGHT,
                        width = charSize.Width * WORLD_SCALE,
                        height = commonHeight * WORLD_SCALE,
                        advance = charAdvances[c] * WORLD_SCALE
                    };

                    _charInfoMap[c] = info;
                    currentX += charWidth;
                    placedCount++;

                    if (placedCount % 500 == 0)
                    {
                        Console.WriteLine($"Placed {placedCount}/{CHARACTER_SET.Length} characters in atlas...");
                    }
                }

                Console.WriteLine($"Successfully placed {placedCount}/{CHARACTER_SET.Length} characters in atlas");

                // GPU에 업로드
                UploadToGPU(atlasBitmap);
            }

            _isInitialized = true;
        }

        private void UploadToGPU(Bitmap bitmap)
        {
            if (_textureId != 0)
            {
                Gl.DeleteTextures(_textureId);
            }

            _textureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _textureId);

            // ✅ 개선된 필터링 설정
            // Anisotropic Filtering 지원 확인
            bool supportsAnisotropic = Gl.CurrentExtensions != null &&
                Gl.CurrentExtensions.TextureFilterAnisotropic_EXT;

            // Min filter: 멀어질 때 사용 (밉맵 + Linear)
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMinFilter,
                Gl.LINEAR_MIPMAP_LINEAR);  // Trilinear filtering

            // Mag filter: 가까워질 때 사용
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMagFilter,
                Gl.LINEAR);

            // ✅ Anisotropic Filtering 적용 (가장 중요!)
            if (supportsAnisotropic)
            {
                float maxAniso = 0;
                Gl.Get(Gl.MAX_TEXTURE_MAX_ANISOTROPY, out maxAniso);

                // 최대 16x까지 사용 (품질과 성능 균형)
                float aniso = Math.Min(16.0f, maxAniso);
                Gl.TexParameter(TextureTarget.Texture2d,
                    TextureParameterName.TextureMaxLod,
                    aniso);

                Console.WriteLine($"Anisotropic filtering enabled: {aniso}x");
            }

            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapS,
                Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapT,
                Gl.CLAMP_TO_EDGE);

            System.Drawing.Imaging.BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba,
                bitmap.Width,
                bitmap.Height,
                0,
                PixelFormat.Bgra,
                PixelType.UnsignedByte,
                bmpData.Scan0
            );

            bitmap.UnlockBits(bmpData);

            // ✅ 밉맵 생성 (필수!)
            Gl.GenerateMipmap(TextureTarget.Texture2d);

            Gl.BindTexture(TextureTarget.Texture2d, 0);

            Console.WriteLine($"Atlas texture uploaded with mipmaps and filtering");
        }

        public void SaveAtlasToFile(string filename)
        {
            try
            {
                string directory = System.IO.Path.GetDirectoryName(filename);
                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                if (!filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    filename += ".png";
                }

                using (Bitmap atlasBitmap = new Bitmap(ATLAS_WIDTH, ATLAS_HEIGHT))
                using (Graphics g = Graphics.FromImage(atlasBitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    g.Clear(Color.Black);

                    StringFormat format = StringFormat.GenericTypographic;

                    // 글자 렌더링 + 경계선 표시
                    using (Pen boundsPen = new Pen(Color.Red, 1))
                    {
                        int count = 0;
                        foreach (var kvp in _charInfoMap)
                        {
                            CharInfo info = kvp.Value;

                            // 픽셀 좌표로 변환
                            float pixelX = info.uvX * ATLAS_WIDTH;
                            float pixelY = info.uvY * ATLAS_HEIGHT;
                            float pixelWidth = info.uvWidth * ATLAS_WIDTH;
                            float pixelHeight = info.uvHeight * ATLAS_HEIGHT;

                            // 글자 렌더링
                            using (SolidBrush brush = new SolidBrush(Color.White))
                            {
                                g.DrawString(kvp.Key.ToString(), _font, brush,
                                    pixelX, pixelY, format);
                            }

                            // 경계선 표시
                            g.DrawRectangle(boundsPen, pixelX, pixelY, pixelWidth, pixelHeight);

                            count++;
                            if (count % 500 == 0)
                            {
                                Console.WriteLine($"Saved {count}/{_charInfoMap.Count} characters to image...");
                            }
                        }
                    }

                    atlasBitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"Atlas saved to: {System.IO.Path.GetFullPath(filename)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving atlas: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            if (_textureId != 0)
            {
                Gl.DeleteTextures(_textureId);
                _textureId = 0;
            }

            if (_font != null)
            {
                _font.Dispose();
                _font = null;
            }

            _charInfoMap.Clear();
            _isInitialized = false;
        }

        public static void Cleanup()
        {
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null;
                Console.WriteLine("CharacterTextureAtlas cleaned up");
            }
        }
    }
}