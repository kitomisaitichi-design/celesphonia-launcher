"""Exercise cooperative launcher replacement, including a busy owner and startup race."""
import ctypes,json,pathlib,subprocess,time
HOME=pathlib.Path(__file__).resolve().parent.parent
EXE=HOME/'Celesphonia Portable Switcher.exe'
REPORT=HOME/'launcher-shell-test-results.json'
checks=[];processes=[]
def wait(predicate,timeout=15):
 end=time.monotonic()+timeout
 while time.monotonic()<end:
  if predicate():return
  time.sleep(.08)
 raise AssertionError('Timed out waiting for launcher replacement')
def output():
 try:return int(REPORT.read_text())
 except (OSError,ValueError):return None
def start(busy=0):
 p=subprocess.Popen([str(EXE),'--instance-probe',str(REPORT),str(busy)])
 processes.append(p);return p
def close(p):
 if p.poll() is not None:return
 kernel=ctypes.WinDLL('kernel32',use_last_error=True)
 kernel.OpenEventW.argtypes=[ctypes.c_ulong,ctypes.c_bool,ctypes.c_wchar_p];kernel.OpenEventW.restype=ctypes.c_void_p
 kernel.SetEvent.argtypes=[ctypes.c_void_p];kernel.CloseHandle.argtypes=[ctypes.c_void_p]
 signal=kernel.OpenEventW(2,False,'Local\\CelesphoniaLauncherReplaceFixture.'+str(p.pid))
 if signal:
  kernel.SetEvent(signal);kernel.CloseHandle(signal);p.wait(timeout=15);return
 callback=ctypes.WINFUNCTYPE(ctypes.c_bool,ctypes.c_void_p,ctypes.c_void_p)
 @callback
 def visit(hwnd,_):
  pid=ctypes.c_ulong();ctypes.windll.user32.GetWindowThreadProcessId(hwnd,ctypes.byref(pid))
  if pid.value==p.pid:ctypes.windll.user32.PostMessageW(hwnd,0x0010,0,0)
  return True
 ctypes.windll.user32.EnumWindows(visit,0)
 p.wait(timeout=15)
try:
 config_hash=None
 import hashlib
 config=HOME/'profiles.json'
 if config.exists():config_hash=hashlib.sha256(config.read_bytes()).hexdigest()
 subprocess.run([str(EXE),'--menu-test',str(REPORT)],check=True)
 menus=json.loads(REPORT.read_text(encoding='utf-8-sig'));assert menus['status']=='PASS' and menus['nativeCloseCycles']==30
 checks.append('Native menu close permits post-close handle access, then releases all 30 popups')
 subprocess.run([str(EXE),'--folder-report',str(REPORT)],check=True)
 folder_report=json.loads(REPORT.read_text(encoding='utf-8-sig'))
 for profile in folder_report:
  folders={f['Label']:f for f in profile['folders']}
  root=pathlib.Path(profile['root']);web=root/'www' if (root/'www').is_dir() else root
  assert pathlib.Path(folders['Game folder']['Path'])==root
  assert pathlib.Path(folders['Game content']['Path'])==web
  assert pathlib.Path(folders['Game data']['Path'])==web/'data'
  assert pathlib.Path(folders['Game plugins']['Path'])==web/'js/plugins'
  assert pathlib.Path(folders['Saves']['Path'])==pathlib.Path(profile['save'])
  assert pathlib.Path(folders['Save backups']['Path'])==HOME/'Save Backups'/profile['id']
  assert pathlib.Path(folders['This version\'s runtime']['Path'])==HOME/'Runtime'/profile['id']
  assert all(f['Exists']==pathlib.Path(f['Path']).is_dir() for f in profile['folders'])
 checks.append('Native folder targets match every profile, including Maple without www; absent folders stay disabled')
 a=start();wait(lambda:output()==a.pid)
 b=start();wait(lambda:output()==b.pid);a.wait(timeout=15)
 assert a.returncode==0 and b.poll() is None
 checks.append('Idle owner closes normally; new launcher becomes the only owner')
 close(b)
 c=start(2200);wait(lambda:output()==c.pid);began=time.monotonic();d=start()
 time.sleep(.45);assert c.poll() is None and output()==c.pid
 wait(lambda:output()==d.pid);c.wait(timeout=15)
 elapsed=time.monotonic()-began;assert elapsed>=1.5 and c.returncode==0
 checks.append('Busy owner remains open until its operation completes; replacement waits')
 close(d)
 e=start();f=start();wait(lambda:output() in (e.pid,f.pid))
 wait(lambda:(e.poll() is None) != (f.poll() is None),20)
 winner=e if e.poll() is None else f;loser=f if winner is e else e
 wait(lambda:output()==winner.pid);assert loser.returncode==0
 checks.append('Simultaneous startups serialize and settle on one window')
 close(winner)
 assert all(p.poll()==0 for p in processes)
 if config_hash:assert hashlib.sha256(config.read_bytes()).hexdigest()==config_hash
 checks.append('Every fixture exits cooperatively without terminating any process')
 REPORT.write_text(json.dumps({'status':'PASS','checks':checks,'menuLifetime':menus,'folderProfilesChecked':len(folder_report),'profilesConfigPreserved':True,'busyWaitSeconds':round(elapsed,2),'fixtureProcesses':len(processes),'forcedTermination':False},indent=2),encoding='utf8')
 print(REPORT.read_text())
finally:
 for p in processes:close(p)
