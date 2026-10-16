"""Read-only Celesphonia text inventory, reference alignment, and overlay pack builder.
Never writes into a game directory. Translation keys are original displayed strings.
"""
import base64,collections,difflib,hashlib,json,pathlib,re,sys
HOME=pathlib.Path(__file__).resolve().parent
if HOME.name=='Source':HOME=HOME.parent
CJK=re.compile(r'[\u3040-\u30ff\u3400-\u9fff]')
CONTROL=re.compile(r'(?:\\|\x1b)(?:[A-Za-z]+(?:\[[^\]\r\n]*\]|<[^>\r\n]*>)?|[.$|!><^{}\\])|%[1-9]|<(?:/?[A-Za-z][^>\r\n]*)>|(?:if|en|cmpd|cmxs)\([^\r\n]*\)')
DB={'Actors':('name','nickname','profile'),'Classes':('name',),'Skills':('name','description','message1','message2'),'Items':('name','description'),'Weapons':('name','description'),'Armors':('name','description'),'Enemies':('name',),'States':('name','message1','message2','message3','message4')}
CUSTOM={'SystemMessage','VariableIconName','LoseMessage','BattleMessage','psmessage','PervertTemplate','ParameterBase','ClothData','HypnosisBase','TagSkillList'}
IDS=('gorpg-1-23','maple-1-7-2','three-in-one-3-3-4','three-in-one-3-3-5-trial','three-in-one-3-3-5-v1-01')
NAMES={'gorpg-1-23':'Gorpg 1.23','maple-1-7-2':'Maple 1.7.2','three-in-one-3-3-4':'Three-in-one 3.3.4','three-in-one-3-3-5-trial':'Three-in-one 3.3.5 trial','three-in-one-3-3-5-v1-01':'Three-in-one 3.3.5 v1.01'}
if (HOME/'version-fingerprints.json').exists():
 _registered=json.loads((HOME/'version-fingerprints.json').read_text('utf-8-sig'))['Versions']
 IDS=tuple(v['Id'] for v in _registered if v['Id']!='ch-current' and all((HOME/'English'/v['Id']/n).is_file() for n in ('manifest.json','dictionary.json','catalog.json')))
 NAMES.update({v['Id']:v['Name'] for v in _registered})
UI_PLUGINS={'GraphicalDesignMode','HypInfo','HypEnemyInfo','GT_ObjectInfoWindow','OriginalCommands','Ticker','TickerSystem','TickerBattleMessage','TMSoloMenu','TMMagicSlot'}
JS_TOKEN=re.compile(r'''("(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|`(?:\\.|[^`\\])*`)|//[^\n]*|/\*[\s\S]*?\*/''')

def jsstring(token):
 """Decode a JavaScript string literal without executing game/plugin code."""
 def escape(m):
  s=m.group(1)
  if s.startswith(('u','x')):return chr(int(s[1:],16))
  return {'n':'\n','r':'\r','t':'\t','b':'\b','f':'\f','v':'\v','\n':'','\r\n':''}.get(s,s)
 return re.sub(r'\\(u[0-9a-fA-F]{4}|x[0-9a-fA-F]{2}|\r\n|[\s\S])',escape,token[1:-1])

def parametertexts(obj,path=(),depth=0):
 # RPG Maker parameters often contain JSON strings inside JSON strings.
 if depth>8:return
 if isinstance(obj,str):
  s=obj.strip()
  if s.startswith(('{','[')):
   try:
    yield from parametertexts(json.loads(s),path,depth+1);return
   except ValueError:pass
  if len(s)>1 and s[0] in ('"',"'") and s[-1]==s[0]:obj=jsstring(s)
  yield path,obj
 elif isinstance(obj,list):
  for i,v in enumerate(obj):yield from parametertexts(v,path+(i,),depth+1)
 elif isinstance(obj,dict):
  for k,v in obj.items():yield from parametertexts(v,path+(k,),depth+1)
def sha(b):return hashlib.sha256(b).hexdigest()
def dump(p,obj):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(obj,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def webroot(root):return (root/load(root/'package.json')['main']).parent
def dataread(p):
 b=p.read_bytes()
 try:return json.loads(b.decode('utf-8-sig'))
 except (ValueError,UnicodeError):
  x=base64.b64decode(b,validate=True);key=b'maple'
  return json.loads(bytes(c^key[i%len(key)] for i,c in enumerate(x)).decode('utf-8-sig'))
def displayed(s):
 if not isinstance(s,str) or not s.strip():return False
 # Conditional-choice plugins append executable visibility/enabling guards
 # to human captions. Validate the caption, retaining the guard verbatim.
 s=re.sub(r'\s+(?:if|en)\([^\r\n]*\)\s*$','',s)
 if re.match(r'^\s*<[^>]+>\s*$',s) or re.search(r'\.(?:png|rpgmvp|ogg|rpgmvo|m4a|rpgmvm|json|js|webm|mp4|ttf)(?:$|[\s"\'])',s,re.I):return False
 if re.search(r'(?:\$game|SceneManager|DataManager|prototype\.|function\s*\(|=>|https?://)',s):return False
 return True
def displayfield(s,kind):
 # Native message/choice commands are presentation evidence by themselves.
 # A dialogue line mentioning a filename, URL or script term is still dialogue.
 if kind in ('message','dialogue','dialogue-block','choice','choice-help','choice-help-block'):
  return isinstance(s,str) and bool(s.strip())
 return displayed(s)
def compatible(a,b):return a.count('\n')==b.count('\n') and CONTROL.findall(a)==CONTROL.findall(b)

def translationcovered(source,dictionary):
 """Count only complete visible targets, retaining native control arguments."""
 def complete(s):
  return s in dictionary and compatible(s,dictionary[s]) and not CJK.search(CONTROL.sub('',dictionary[s]))
 return not CJK.search(CONTROL.sub('',source)) or complete(source) or all(complete(line) or not CJK.search(CONTROL.sub('',line)) for line in source.split('\n'))
def at(obj,path):
 for key in path:obj=obj[key]
 return obj
def walk(obj,path=()):
 if isinstance(obj,str):yield path,obj
 elif isinstance(obj,list):
  for i,v in enumerate(obj):yield from walk(v,path+(i,))
 elif isinstance(obj,dict):
  for k,v in obj.items():yield from walk(v,path+(k,))
def commandlists(obj,path=()):
 if isinstance(obj,list):
  if obj and isinstance(obj[0],dict) and 'code' in obj[0] and 'parameters' in obj[0]:yield path,obj
  else:
   for i,v in enumerate(obj):yield from commandlists(v,path+(i,))
 elif isinstance(obj,dict):
  for k,v in obj.items():yield from commandlists(v,path+(k,))
def eventfields(cmd):
 code=cmd.get('code');p=cmd.get('parameters',[])
 if code in (401,405) and p:yield (0,),p[0],'dialogue'
 elif code==102 and p and isinstance(p[0],list):
  for i,v in enumerate(p[0]):yield (0,i),v,'choice'
 elif code in (320,324,325) and len(p)>1:yield (1,),p[1],'character'
 elif code==122 and len(p)>4 and isinstance(p[4],str):
  # Literal string values later used by profile/picture displays. Inventory only;
  # the variable command, expression and saved value remain untouched.
  token=JS_TOKEN.fullmatch(p[4].strip()) if p[3]==4 else None
  if token and token.group(1):
   text=jsstring(token.group(1))
   if CJK.search(text) and displayed(text):yield ('literal',0),text,'display-value'
  elif p[3]==0 and CJK.search(p[4]) and displayed(p[4]):yield (4,),p[4],'display-value'
 elif code==356 and p and isinstance(p[0],str):
  # HzChoiceEvent uses event names as stable interaction identifiers AND draws
  # them as map captions. Inventory display copies, retaining command arguments.
  m=re.match(r'(?i)^HzChoiceCustom\s+(\S+)\s+(message|color|disabled)(?:\s+(.*))?$',p[0])
  if m:
   yield ('hz-name',),m.group(1),'map-choice-name'
   if m.group(2).lower()=='message' and m.group(3):
    yield ('hz-help',),m.group(3).replace('\\n','\n'),'map-choice-help'
  m=re.match(r'(?i)^D_TEXT\s+(.*?)(?:\s+\d+)?$',p[0])
  if m:yield (0,),m.group(1),'plugin-display'
  m=re.match(r'^CallPurpose\s+[1-6]\s+(.+)$',p[0])
  if m:yield (0,),m.group(1),'objective'
  m=re.match(r'^TMng\s+\S+\s+(.+)$',p[0])
  if m:yield (0,),m.group(1),'notification'
  m=re.match(r'^greedCommand\s+(.+)$',p[0])
  if m:yield (0,),m.group(1),'plugin-display'
 elif code in (355,655) and p and isinstance(p[0],str):
  s=p[0]
  literal=r'("(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\')'
  for m in re.finditer(r"BattleManager\._logWindow\.push\s*\(\s*['\"]addText['\"]\s*,\s*"+literal,s):
   text=jsstring(m.group(1))
   if CJK.search(text):yield ('literal',m.start(1)),text,'battle-log'
  for m in re.finditer(r"this\.pluginCommand\s*\(\s*['\"]TMng['\"]\s*,\s*\[\s*['\"][^'\"]+['\"]\s*,\s*"+literal,s):
   text=jsstring(m.group(1))
   if CJK.search(text):yield ('literal',m.start(1)),text,'script-notification'
  for m in re.finditer(r"\$CM_value\.set\s*\(\s*(['\"])(阴部的称呼|胸部的称呼|课程名|主人名字)\1\s*,\s*"+literal,s):
   text=jsstring(m.group(3))
   if CJK.search(text):yield ('literal',m.start(3)),text,'stored-display-value'
  # Live comments are a distinct native display consumer. Index only a literal
  # first argument; resource names, colors and dynamic expressions stay native.
  for m in re.finditer(r'\bLCM\.show\s*\(\s*("(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\')',s):
   text=jsstring(m.group(1))
   if CJK.search(text) and displayed(text):yield ('literal',m.start(1)),text,'live-comment'
  if re.search(r'\$gameVariables(?:\._data\[|\.setValue\s*\()',s):
   for m in JS_TOKEN.finditer(s):
    if not m.group(1) or '${' in m.group(1):continue
    text=jsstring(m.group(1))
    if CJK.search(text) and displayed(text):yield ('literal',m.start()),text,'display-value'
  if re.search(r'(?:\$gameMessage\.add|\.drawText(?:Ex)?|\.addCommand|\.setName|TickerManager\.show)\s*\(',s):
   for m in JS_TOKEN.finditer(s):
    if not m.group(1) or '${' in m.group(1):continue
    v=jsstring(m.group(1))
    if CJK.search(v) and displayed(v):yield ('literal',m.start()),v,'script-display'

def choicehelp(commands):
 """MPP_ChoiceEX renders only continuation comments under its help marker."""
 for i,cmd in enumerate(commands):
  if cmd.get('code')!=108 or cmd.get('parameters',[])!=['選択肢ヘルプ']:continue
  lines=[];j=i+1
  while j<len(commands) and commands[j].get('code')==408:
   text=commands[j]['parameters'][0];lines.append(text);yield j,text,'choice-help';j+=1
  if len(lines)>1:yield i,'\n'.join(lines),'choice-help-block'
def cmdsig(c):
 p=json.loads(json.dumps(c.get('parameters',[])));code=c.get('code')
 for path,s,kind in eventfields(c):
  if 'literal' in path or any(str(x).startswith('hz-') for x in path):continue
  a=p
  for k in path[:-1]:a=a[k]
  value=CONTROL.findall(s) if isinstance(s,str) else None
  if code==356 and isinstance(p[0],str):
   prefix=p[0][:p[0].find(s)] if s in p[0] else p[0].split(' ')[0]
   value=[prefix.strip(),value]
  a[path[-1]]=value
 return json.dumps([code,c.get('indent'),p],ensure_ascii=False,separators=(',',':'))

def randommessagefields(commands):
 """Trace literal buffers only when the complete block feeds MessageRandom3."""
 found={};i=0
 pattern=re.compile(r'^\s*var\s+(p[123])\s*=\s*("(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\')\s*;?\s*$')
 while i<len(commands):
  if commands[i].get('code')!=355:i+=1;continue
  start=i;i+=1
  while i<len(commands) and commands[i].get('code')==655:i+=1
  block=commands[start:i]
  if len(block)!=4:continue
  tail=block[-1].get('parameters',[''])[0]
  if not re.fullmatch(r'\s*this\.pluginCommand\("MessageRandom3",\s*\[p1,p2,p3\]\);\s*(?://[^\n]*)?',tail):continue
  values=[]
  for j,cmd in enumerate(block[:3]):
   m=pattern.fullmatch(cmd.get('parameters',[''])[0])
   if not m or m.group(1)!='p'+str(j+1):break
   values.append((start+j,('literal',m.start(2)),jsstring(m.group(2)),'random-message-buffer'))
  if len(values)==3:
   for pos,path,text,kind in values:found[pos]=[(path,text,kind)]
 return found

def atoms(commands):
 result=[];i=0
 buffered=randommessagefields(commands)
 while i<len(commands):
  c=commands[i];code=c.get('code')
  if code in (401,405):
   lines=[];start=i
   while i<len(commands) and commands[i].get('code')==code:lines.append(commands[i]['parameters'][0]);i+=1
   text='\n'.join(lines)
   result.append({'sig':json.dumps([code,c.get('indent'),CONTROL.findall(text)],ensure_ascii=False),'fields':[((0,),text,'message')],'code':code,'start':start})
  else:
   result.append({'sig':cmdsig(c),'fields':list(eventfields(c))+buffered.get(i,[]),'code':code,'start':i});i+=1
 return result

def reflow(reference,source):
 """Reflow a complete English message, keeping all words, control tokens and source line count."""
 if CONTROL.findall(reference)!=CONTROL.findall(source):return None
 if reference.count('\n')==source.count('\n'):return reference
 if '\n\n' in reference or '\n\n' in source:return None
 flat=' '.join(reference.split('\n'));needed=source.count('\n')
 if needed==0:return flat
 protected=[m.span() for m in CONTROL.finditer(flat)]
 positions=[m for m in re.finditer(r'[ \u2008]+',flat) if not any(a<=m.start()<b for a,b in protected)]
 if len(positions)<needed:return None
 chosen=[];last=-1
 for i in range(needed):
  options=[m for j,m in enumerate(positions) if j>last and len(positions)-j>=needed-i]
  if not options:return None
  m=min(options,key=lambda m:abs(m.start()-len(flat)*(i+1)/(needed+1)));last=positions.index(m);chosen.append(m)
 out=flat
 for m in reversed(chosen):out=out[:m.start()]+'\n'+out[m.end():]
 return out if compatible(source,out) else None

def reflowparagraphs(reference,source):
 """Preserve glossary paragraph boundaries while fitting English source lines."""
 aa=reference.split('\n\n');bb=source.split('\n\n')
 if len(aa)!=len(bb):return None
 values=[reflow(a,b) for a,b in zip(aa,bb)]
 if any(x is None for x in values):return None
 value='\n\n'.join(values)
 return value if compatible(source,value) else None
def inventory(web):
 slots={};objects={};hashes={};events={};errors=[]
 def add(file,path,s,kind,context=None):
  if displayfield(s,kind):
   slots[(file,path)]={'source':s,'kind':kind,'context':context or file+'/'+ '/'.join(map(str,path))}
   if kind=='plugin-ui':
    plain=CONTROL.sub('',s)
    for i,m in enumerate(re.finditer(r'(?:HP|MP|SP|TP)?[\u3040-\u30ff\u3400-\u9fff]+',plain)):
     if len(m.group())<=24:slots[(file,path+('label',i))]={'source':m.group(),'kind':'ui-label','context':file+'/'+ '/'.join(map(str,path))+'/label/'+str(i)}
   if kind in ('plugin-display','display-table','display-value'):
    for i,m in enumerate(re.finditer(r'\[[\u3040-\u30ff\u3400-\u9fff]+\]',s)):
     slots[(file,path+('requirement-label',i))]={'source':m.group(),'kind':'ui-label','context':file+'/'+ '/'.join(map(str,path))+'/requirement-label/'+str(i)}
    for i,m in enumerate(re.finditer(r'<((?:A\.)?[\u3040-\u30ff\u3400-\u9fff][^<>\\\x1b\r\n]*)>',s)):
     # These are human captions in picture/table text. Store the inner caption
     # separately so an English word is never mistaken for an executable tag.
     slots[(file,path+('picture-label',i))]={'source':m.group(1),'kind':'picture-label','context':file+'/'+ '/'.join(map(str,path))+'/picture-label/'+str(i)}
 for f in sorted((web/'data').glob('*.json')):
  try:o=dataread(f)
  except Exception as e:errors.append({'file':f.name,'error':type(e).__name__});continue
  name=f.stem;objects[f.name]=o;hashes['data/'+f.name]=sha(f.read_bytes())
  if name.startswith('Map') and isinstance(o,dict):
   add(f.name,('displayName',),o.get('displayName'),'map-name')
   for i,m in enumerate(re.finditer(r'<MapName:([^>\r\n]+)>',o.get('note',''))):add(f.name,('map-name',i),m.group(1),'map-name')
   for event in o.get('events',[]) or []:
    if not isinstance(event,dict):continue
    if re.search(r'<hzChoice\s*:',event.get('note',''),re.I):
     add(f.name,('events',event['id'],'hz-name'),event.get('name'),'map-choice-name')
    for i,m in enumerate(re.finditer(r'<namePop:([^>\r\n]+)>',event.get('note',''),re.I)):
     add(f.name,('events',event['id'],'name-pop',i),m.group(1),'map-label')
  if name in DB and isinstance(o,list):
   for i,row in enumerate(o):
    if not isinstance(row,dict):continue
    for field in DB[name]:
     if field in row:add(f.name,(i,field),row[field],'character' if name=='Actors' else 'database')
    if name=='Enemies':
     for j,m in enumerate(re.finditer(r'<desc(\d+):([^>]*)>',row.get('note',''),re.I)):
      add(f.name,(i,'enemy-description',m.group(1),j),m.group(2),'enemy-description')
    if name in ('Weapons','Armors'):
     for j,m in enumerate(re.finditer(r'<option:([^>]*)>',row.get('note',''),re.I)):
      add(f.name,(i,'equipment-option',j),m.group(1),'equipment-option')
    if name=='States' and re.search(r'<nameDisplay\s*>',row.get('note',''),re.I):
     add(f.name,(i,'enemy-name-prefix'),row.get('name'),'enemy-prefix')
    # Ticker item pop-ups deliberately display only the first description line.
    # Catalog that rendered text independently of the complete item help panel.
    description=row.get('description')
    if isinstance(description,str) and '\n' in description:
     add(f.name,(i,'description-first-line'),description.split('\n')[0].rstrip('\r'),'database')
    # Only human-facing note values are inventoried. Quest descriptions and
    # glossary categories are rendered by SceneGlossary; note logic stays intact.
    for j,m in enumerate(re.finditer(r'<SG(説明\d*|Description\d*|カテゴリ|Category):([^>]*)>',row.get('note',''),re.I)):
     tag=m.group(1);kind='glossary-description' if re.match(r'説明|Description',tag,re.I) else 'glossary-category'
     add(f.name,(i,'glossary-text',tag,j),m.group(2),kind)
    for j,m in enumerate(re.finditer(r'<txt:([^>\r\n]+)>',row.get('note',''),re.I)):
     add(f.name,(i,'activation-text',j),m.group(1),'notification')
     add(f.name,(i,'activation-text-trimmed',j),m.group(1).strip(),'notification')
  if name=='System' and isinstance(o,dict):
   for k in ['currencyUnit','gameTitle']:add(f.name,(k,),o.get(k),'menu')
   for k in ['terms','elements','skillTypes','weaponTypes','armorTypes','equipTypes']:
    for path,s in walk(o.get(k,{})):
     add(f.name,(k,)+path,s,'menu')
     if k in ('weaponTypes','armorTypes','equipTypes'):add(f.name,(k,)+path+('human-caption',),s,'picture-label')
   for k in ['variables','switches']:
    for path,s in walk(o.get(k,{})):add(f.name,(k,)+path,s,'variable-label')
  if name in CUSTOM:
   for path,s in walk(o):
    add(f.name,path,s,'display-table')
    # Parameter names are interpolated into <...> captions by the profile
    # picture renderer. Keep their human-label provenance separate from tags.
    if f.name=='ParameterBase.json' and path and path[-1]=='ParaName':
     add(f.name,path,s,'picture-label')
    # Brackets here decorate a visible label, unlike executable plugin tags.
    if isinstance(s,str):
     visible=re.sub(r'(?:\\|\x1b)[CcIi]\[\d+\]','',s).strip()
     if re.fullmatch(r'<[^<>\\\x1b]+>',visible) and CJK.search(visible):add(f.name,path+('visible-label',),visible,'display-table')
  # Native CE416/418 read these exact fields for proximity/touch trap tickers.
  # Other trap properties control IDs, damage and behavior and stay metadata.
  if name=='trap_list' and isinstance(o,dict):
   for trapid,row in o.items():
    if not isinstance(row,dict):continue
    for field in ('TrapName','TrapMemo'):
     if isinstance(row.get(field),str):add(f.name,(trapid,field),row[field],'display-table')
  for path,commands in commandlists(o):
   events[(f.name,path)]=commands
   speaker='';buffered=randommessagefields(commands)
   for i,c in enumerate(commands):
    if c.get('code')==101:speaker=str(c.get('parameters',[])[:2])
    for tail,s,kind in list(eventfields(c))+buffered.get(i,[]):
     add(f.name,path+(i,'parameters')+tail,s,kind,(f.name+'/'+ '/'.join(map(str,path))+'/command/'+str(i)+' speaker='+speaker))
     if isinstance(s,str):
      label=re.match(r'^(?:\\|\x1b)[Nn]<([^<>\r\n]+)>',s)
      if label and CJK.search(label[1]):
       # Name windows display this payload independently. Allow actor-name
       # substitutions, but never catalog another executable escape payload.
       literal=re.sub(r'(?:\\|\x1b)[Nn]\[\d+\]','',label[1])
       add(f.name,path+(i,'speaker-label'),label[1],'character')
       # Some native name boxes put a literal title inside a nonnumeric
       # actor escape. The renderer displays that title independently; it
       # needs its own catalog entry rather than an executable-code entry.
       nested=re.fullmatch(r'(?:\\|\x1b)[Nn]\[([^\]\\\r\n]+)\]',label[1])
       if nested and CJK.search(nested[1]):add(f.name,path+(i,'speaker-literal'),nested[1],'character')
   i=0
   for position,text,kind in choicehelp(commands):add(f.name,path+(position,kind),text,kind)
   while i<len(commands):
    if commands[i].get('code') not in (401,405):i+=1;continue
    code=commands[i]['code'];start=i;lines=[]
    while i<len(commands) and commands[i].get('code')==code:lines.append(commands[i]['parameters'][0]);i+=1
    if len(lines)>1:add(f.name,path+(start,'block'), '\n'.join(lines),'dialogue-block')
 # Mods also add visible commands directly to RPG Maker's core window code.
 # Read literal first arguments at presentation calls; never edit the script,
 # option symbols, configuration properties, or expressions.
 for core in ('rpg_windows.js','rpg_scenes.js'):
  file=web/'js'/core
  if not file.exists():continue
  source=file.read_text(encoding='utf-8-sig')
  for m in JS_TOKEN.finditer(source):
   if not m.group(1):continue
   prefix=source[max(0,m.start()-110):m.start()]
   if re.search(r'(?:\.(?:addCommand|drawText|drawTextEx|setText|setHelpText)|TickerManager\.show|\$gameMessage\.add)\s*\(\s*$',prefix):
    text=jsstring(m.group(1))
    if CJK.search(text):add('js/'+core,('display-literal',m.start()),text,'core-ui')
 pluginfile=web/'js/plugins.js'
 if pluginfile.exists():
  source=pluginfile.read_text(encoding='utf-8-sig');plugins=json.loads(source[source.index('['):source.rindex(']')+1])
  for plugin in plugins:
   if not plugin.get('status'):continue
   name=plugin['name'];filename='js/plugins/'+name+'.js'
   for path,text in parametertexts(plugin.get('parameters',{})):
    context='/'.join(map(str,path))
    # The 1.051 creator release adds actual controller-menu captions under
    # Mano_GamePadConfig/textSymbol6–8. These are human-facing descriptions,
    # not the runtime symbol IDs excluded by the generic Symbol guard.
    gamepad_caption=(name=='Mano_GamePadConfig' and path
                     and str(path[0]) in ('textSymbol6','textSymbol7','textSymbol8'))
    if CJK.search(text) and (gamepad_caption or not re.search(r'(?:Bind|Symbol|Script|Eval|Formula|SceneName|ColorCategory|ファイル)',context,re.I)):
     add('js/plugins.js',(name,)+path,text,'plugin-ui')
   f=web/filename
   if not f.exists():continue
   source=f.read_text(encoding='utf-8-sig')
   # Maple builds the same map-discovery notification with concatenation
   # instead of a template literal. Recognize this presentation call only.
   quoted=r'''(?:"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')'''
   concatenated=r'TickerManager\.show\(\s*('+quoted+r')\s*\+\s*[A-Za-z_$][\w$]*(?:\.[A-Za-z_$][\w$]*)?\s*\+\s*('+quoted+r')\s*,'
   for match in re.finditer(concatenated,source):
    text=jsstring(match[1])+'%1'+jsstring(match[2])
    if CJK.search(text):add(filename,('ticker-concatenation',match.start()),text,'notification')
   for m in JS_TOKEN.finditer(source):
    if not m.group(1):continue # Comments and metadata are never translation sources.
    text=jsstring(m.group(1))
    nearby=source[max(0,m.start()-110):m.start()]
    # Three-in-One's human-facing window/save-slot titles are JS literals
    # (some characters are \u-escaped), not ordinary drawText calls. Index
    # only these explicit display variables. Never rewrite the plugin or
    # its savefile title comparisons: the overlay translates rendering only.
    if name=='MyselfPlugins' and CJK.search(text) and re.search(
      r'\b(?:var|let|const)\s+(?:newtitle|savetitle|autosavetitle)\s*=\s*$',nearby):
     add(filename,('title-literal',m.start()),text,'window-title')
    if m.group(1).startswith('`') and re.search(r'TickerManager\.show\s*\(\s*$',nearby):
     tokens=re.findall(r'\$\{([A-Za-z_$][\w$]*)\}',text)
     if tokens and len(set(tokens))<=9:
      names=list(dict.fromkeys(tokens));template=re.sub(r'\$\{([A-Za-z_$][\w$]*)\}',lambda x:'%'+str(names.index(x[1])+1),text)
      if '${' not in template:add(filename,('ticker-template',m.start()),template,'notification')
    if CJK.search(text) and (name in UI_PLUGINS or re.search(r'(?:drawText(?:Ex)?|addCommand|addText|setText|TickerManager\.show)\s*\([^;\n]*$',nearby)):
     add(filename,('literal',m.start()),text,'plugin-ui')
 # Dynamic strings seen in this exact version supplement the static inventory.
 state=web/'celesphonia-english/state.json';seen=web/'celesphonia-english/seen-untranslated.json'
 if state.exists() and seen.exists():
  for i,text in enumerate(load(seen)):
   text=text.replace('\x1b','\\')
   if len(text)<6000:add('seen-untranslated.json',(i,),text,'seen-display')
 return {'slots':slots,'objects':objects,'hashes':hashes,'events':events,'errors':errors}
def references(cn,en,cache):
 candidates=collections.defaultdict(set);provenance=collections.Counter()
 def add(a,b,why):
  if a!=b and CJK.search(a) and not CJK.search(b) and re.search('[A-Za-z]',b) and compatible(a,b):candidates[a].add(b);provenance[why]+=1
 for key,a in cn['slots'].items():
  b=en['slots'].get(key)
  if b and a['kind']=='menu':add(a['source'],b['source'],'same-menu-slot')
  elif b and a['kind'] in ('character','database','glossary-description','glossary-category') and key[0][:-5] in DB:
   # A shared array index alone is insufficient: compare the nontext row first.
   fields=DB[key[0][:-5]]
   def core(row):return {k:v for k,v in row.items() if k not in fields and k!='note'}
   cr=cn['objects'][key[0]][key[1][0]];er=en['objects'][key[0]][key[1][0]]
   if a['kind'].startswith('glossary-'):
    # Require equal note logic as well as row structure; array position alone
    # cannot align a mod's added or altered quests to the English reference.
    strip=lambda n:re.sub(r'<SG(?:説明\d*|Description\d*|カテゴリ|Category):[^>]*>','',n,flags=re.I)
    if strip(cr.get('note',''))!=strip(er.get('note','')):continue
   if core(cr)==core(er):
    value=reflowparagraphs(b['source'],a['source']) if a['kind']=='glossary-description' else b['source']
    if value is not None:add(a['source'],value,'matching-database-row')
 for key,cmds in cn['events'].items():
  old=en['events'].get(key)
  if not old:continue
  aa=atoms(cmds);bb=atoms(old);xs=[c['sig'] for c in aa];ys=[c['sig'] for c in bb]
  blocks=[(0,0,len(xs))] if xs==ys else [(b.a,b.b,b.size) for b in difflib.SequenceMatcher(None,xs,ys,autojunk=True).get_matching_blocks() if b.size>=3 and any(aa[j]['code'] not in (101,401,405,102,402) for j in range(b.a,b.a+b.size))]
  for a,b,n in blocks:
   for i in range(n):
    f=aa[a+i]['fields'];g=bb[b+i]['fields']
    if len(f)!=len(g):continue
    for (pa,sa,ka),(pb,sb,kb) in zip(f,g):
     if pa!=pb or not isinstance(sa,str) or not isinstance(sb,str):continue
     if ka=='message':
      value=reflow(sb,sa)
      if value is not None:add(sa,value,'anchored-whole-message')
     else:add(sa,sb,'anchored-choice-or-label')
 # MTool's JP -> CN table supplies a correspondence pivot, not an English source.
 for filename,f in cache.get('project',{}).get('files',{}).items():
  basename=pathlib.PurePosixPath(filename.replace('\\','/')).name
  for i,row in enumerate(f.get('data',[])):
   if not isinstance(row,list) or len(row)<2 or not isinstance(row[1],str):continue
   for ctx in (f.get('context',[])[i] if i<len(f.get('context',[])) else []):
    parts=ctx.split('/')[1:];path=tuple(int(x) if x.isdigit() else x for x in parts)
    a=cn['slots'].get((basename,path));b=en['slots'].get((basename,path))
    if a and b and row[1]==a['source'] and a['kind']=='menu':add(a['source'],b['source'],'mtool-context-pivot')
 resolved={k:next(iter(v)) for k,v in candidates.items() if len(v)==1}
 return resolved,{k:sorted(v) for k,v in candidates.items() if len(v)>1},dict(provenance)
def main():
 config=load(HOME/'profiles.json');index=load(HOME/'version-fingerprints.json');roots={};english=None
 for p in config['Profiles']:
  root=pathlib.Path(p['Root']);web=webroot(root)
  for v in index['Versions']:
   if all(sha((web/k).read_bytes())==h for k,h in v['Fingerprints'].items()):
    roots.setdefault(v['Id'],web)
    if v['Id']=='ch-current' and english is None:english=web
 if english is None:raise RuntimeError('English reference not found')
 variants={id:inventory(roots[id]) for id in IDS};ref=inventory(english)
 cachepath=roots['gorpg-1-23'].parent/'translation.trans';cache=load(cachepath) if cachepath.exists() else {}
 shared,ambiguous,reuse=references(variants['gorpg-1-23'],ref,cache)
 reviewedfile=HOME/'English'/'reviewed-translations.json';reviewed=load(reviewedfile) if reviewedfile.exists() else {}
 for k,v in reviewed.items():
  if not isinstance(v,str) or not compatible(k,v):raise RuntimeError('Invalid reviewed translation: '+k[:60])
 out=HOME/'English';report=[];allmissing={};total=collections.Counter()
 for ident in IDS:
  inv=variants[ident];entries={}
  folder=out/ident;priorManifest=load(folder/'manifest.json') if (folder/'manifest.json').exists() else None
  priorDictionary={};priorCatalog={}
  if priorManifest:
   marks=next(v['Fingerprints'] for v in index['Versions'] if v['Id']==ident)
   if priorManifest['Fingerprints']==marks:
    assert priorManifest['DictionaryHash']==sha((folder/'dictionary.json').read_bytes())
    assert priorManifest['CatalogHash']==sha((folder/'catalog.json').read_bytes())
    priorDictionary=load(folder/'dictionary.json');priorCatalog=load(folder/'catalog.json')
  for key,slot in inv['slots'].items():
   text=slot['source']
   if not CJK.search(text):continue
   row=entries.setdefault(text,{'source':text,'kinds':[],'contexts':[]})
   if slot['kind'] not in row['kinds']:row['kinds'].append(slot['kind'])
   if len(row['contexts'])<4 and slot['context'] not in row['contexts']:row['contexts'].append(slot['context'])
  dictionary={k:reviewed.get(k,shared.get(k)) for k in entries if k in reviewed or k in shared}
  # Previously verified whole-field grammars, diagnostics and parent projections
  # supplement native discovery. Reindexing must not remove display support.
  for key,target in priorDictionary.items():
   target=reviewed.get(key,target)
   if not compatible(key,target):raise RuntimeError('Invalid prior display projection')
   if key not in dictionary:dictionary[key]=target
  for key,row in priorCatalog.items():
   if key not in entries:entries[key]=row
  # Compose untranslated multiline blocks only when every original line has an exact match.
  for k,row in entries.items():
   if k not in dictionary and '\n' in k and all(line in dictionary or not CJK.search(line) for line in k.split('\n')):
    v='\n'.join(dictionary.get(line,line) for line in k.split('\n'))
    if compatible(k,v):dictionary[k]=v
  # Each current batch is validated independently; untranslated text is left intact.
  missing={k:v for k,v in entries.items() if k not in dictionary}
  for k,v in missing.items():
   row=allmissing.setdefault(k,{'source':k,'variants':[],'kinds':v['kinds'],'contexts':v['contexts']})
   row['variants'].append(ident)
  marks=next(v['Fingerprints'] for v in index['Versions'] if v['Id']==ident)
  manifest={'Format':1,'VersionId':ident,'Name':NAMES[ident]+' English text overlay','Fingerprints':marks,'TotalTexts':len(entries),'TranslatedTexts':len(dictionary),'ReviewedTexts':sum(k in reviewed for k in dictionary),'ReferenceTexts':sum(k not in reviewed for k in dictionary),'Complete':not missing,'Mode':'display-only','SkipNewBaseTranslation':ident=='gorpg-1-23'}
  dump(out/ident/'dictionary.json',dictionary);dump(out/ident/'catalog.json',entries);dump(out/ident/'remaining.json',{k:k for k in missing})
  manifest['DictionaryHash']=sha((out/ident/'dictionary.json').read_bytes());manifest['CatalogHash']=sha((out/ident/'catalog.json').read_bytes())
  dump(out/ident/'manifest.json',manifest)
  report.append({'version':ident,'texts':len(entries),'translated':len(dictionary),'remaining':len(missing),'reference':manifest['ReferenceTexts'],'decodedFiles':len(inv['objects']),'parseErrors':inv['errors']})
  total.update({x:1 for x in inv['errors']}) if False else None
 export=roots['three-in-one-3-3-5-trial'].parent/'ManualTransFile.json';raw=load(export) if export.exists() else {}
 known=set(variants['three-in-one-3-3-5-trial']['slots'][k]['source'] for k in variants['three-in-one-3-3-5-trial']['slots'])
 chraw=load(english.parent/'ManualTransFile.json') if (english.parent/'ManualTransFile.json').exists() else {}
 audit={'exportEntries':len(raw),'exportChangedValues':sum(k!=v for k,v in raw.items()),'exportKeysMatchedDisplayCatalog':len(set(raw)&known),'exportOtherKeys':len(set(raw)-known),'englishReference':'Existing English CH installation and CH MTool export (read-only)','CHExportEntries':len(chraw),'CHExportChangedValues':sum(k!=v for k,v in chraw.items()),'CHExportEnglishKeys':sum(bool(re.search('[A-Za-z]',k)) and not CJK.search(k) for k in chraw),'baseReferenceReuse':reuse,'ambiguousReferenceStrings':len(ambiguous),'variants':report,'uniqueRemaining':len(allmissing),'baseFilesModified':0,'notes':['MTool export mixes display text, editor labels, comments and program strings.','CH export is English-to-itself; it confirms English text but is not a Chinese-to-English lookup.','Maple databases are base64/XOR encoded by its own loader; its item dependency checks stay intact.','English runtime overlay does not mutate game data, variables, scene names, IDs or scripts.','English coverage is partial until remaining translations are supplied and reviewed.']}
 dump(out/'architecture-audit.json',audit);dump(out/'translation-queue.json',allmissing);dump(out/'reference-conflicts.json',ambiguous)
 print(json.dumps(audit,ensure_ascii=False,indent=2))
 # Rebuild the full dialogue backlog whenever the dictionaries change, so
 # omissions are found across all event pages rather than screenshot lists.
 backlog=HOME/'Tests/BuildDialogueBacklog.py'
 if backlog.exists():
  import runpy
  runpy.run_path(str(backlog),run_name='__main__')
 interactions=HOME/'Tests/BuildInteractionsIndex.py'
 if interactions.exists():runpy.run_path(str(interactions),run_name='__main__')
def restrictedsource(file,path,version=None):
 """Current source classification; immutable historical snapshots stay intact.

 CE736–740 were excluded from Chinese patch wording alone. Pass 080 checks
 the corresponding English blocks and numeric adult notice directly. Their
 young-adult forms of address no longer justify the earlier exclusion.
 """
 return False

def neutralrestrictedfield(source,file=None,path=None):
 if file=='CommonEvents.json' and path and path[0]==737:
  return source in {
   '少年看着\\N[100]的眼睛闪闪发光。\n作为守护城市的秘密魔法少女，她在这种小孩的圈子里很有人气。',
   '\\n<\\N[20]>那可不。\n都上新闻了……好厉害，真的在和怪人战斗啊。\n姐姐一个人守护着大家，好厉害啊！'
   ,'\\n<\\N[20]>姐姐好像很舒服……。\n听到姐姐的声音，我……。'
   ,'\\n<\\N[20]>其实很舒服吧？'
   ,'\\n<\\N[20]>姐姐……不，\\n[100]……。\n被欺负会很开心吧？'
   ,'\\N[100]腰部不断颤抖，像是痉挛了一样。\n因为阴蒂被刺激，快感一下子上升了，马上就要到达极限。'
  }
 """User-supplied ordinary NPC lines/labels, independently reviewable."""
 return source in {'中年人','乖巧的女孩','真是舒服···','这不是做的很好嘛。','是的爸爸。'}

if __name__=='__main__':main()
