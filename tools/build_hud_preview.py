#!/usr/bin/env python3
"""Generate a dependency-free browser approximation from the actual Panorama layout.

The browser does not implement Panorama; Workshop Tools and a CS2 client are
still required to verify native rendering and input capture.
"""
from pathlib import Path
import html
import base64
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
LAYOUT = ROOT / 'workshop/panorama/layout/custom_game/sws2cs2menus/menu.xml'
STYLE = ROOT / 'workshop/panorama/styles/custom_game/sws2cs2menus/menu.css'


def element(node):
    tag = {'Panel': 'div', 'Label': 'span', 'Button': 'button', 'Image': 'img'}[node.tag]
    attrs = ''.join(f' {key}="{html.escape(node.attrib[key], quote=True)}"'
                    for key in ('id', 'class') if key in node.attrib)
    if tag == 'img':
        source = node.get('src', '')
        if source.startswith('file://{images}/'):
            path = ROOT / 'workshop/panorama/images' / source.removeprefix('file://{images}/')
            attrs += ' src="data:image/svg+xml;base64,' + base64.b64encode(path.read_bytes()).decode() + '"'
        return f'<img{attrs} alt="">'
    text = node.get('text', '')
    if text.startswith('{s:'):
        text = ''
    return f'<{tag}{attrs}>{html.escape(text)}'+''.join(element(c) for c in node)+f'</{tag}>'


css = STYLE.read_text()
for name in ('players', 'maps', 'warning'):
    icon = ROOT / f'workshop/panorama/images/custom_game/sws2cs2menus/{name}.svg'
    css = css.replace(f'file://{{images}}/custom_game/sws2cs2menus/{name}.svg',
                      'data:image/svg+xml;base64,' + base64.b64encode(icon.read_bytes()).decode())
css = re.sub(r'gradient\(linear, 0% 0%, 100% 100%, from\((#[\da-f]+)\), color-stop\(0.4, (#[\da-f]+)\), to\((#[\da-f]+)\)\)',
             r'linear-gradient(135deg, \1, \2 40%, \3)', css)
css = css.replace('flow-children: down;', 'display: flex; flex-direction: column;')
css = css.replace('flow-children: right;', 'display: flex; flex-direction: row;')
css = css.replace('width: fill-parent-flow(1);', 'flex: 1; min-width: 0; width: auto;')
css = css.replace('height: fill-parent-flow(1);', 'flex: 1; min-height: 0; height: auto;')
css = css.replace('visibility: collapse;', 'display: none !important;')
css = css.replace('box-shadow: fill #000000b0 0px 14px 64px 0px;', 'box-shadow: 0 14px 64px #000000b0;')
css = css.replace('background-color: linear-gradient(', 'background: linear-gradient(')
css = re.sub(r'(horizontal|vertical)-align: [\w-]+;', '', css)
css = re.sub(r"@keyframes '([^']+)'", r'@keyframes \1', css)
body = element(ET.parse(LAYOUT).getroot().find('Panel'))
adapter = '''
* { box-sizing: border-box; flex-shrink: 0; }
body { margin: 0; overflow: hidden; font: 14px "Noto Sans CJK SC", "Microsoft YaHei", sans-serif; background: #111c25; color: #eaf2f5; }
button { position: relative; font: inherit; cursor: pointer; color: inherit; text-align: left; border: 0; }
button:focus-visible { outline: 2px solid #70e0c4; outline-offset: 3px; }
span { display: block; }
div { position: relative; }
button:disabled { cursor: default; }
#stage { width: 1920px; height: 1080px; position: absolute; transform-origin: top left; }
.game { position: absolute; inset: 0; overflow: hidden; background: radial-gradient(ellipse at 60% 35%, #506069, #253541 45%, #101a23 85%); }
.game:before { content: ''; position: absolute; inset: 0; background: repeating-linear-gradient(0deg, transparent, transparent 130px, #b8cdd70a 131px), repeating-linear-gradient(90deg, transparent, transparent 160px, #b8cdd70a 161px); transform: perspective(550px) rotateX(35deg) scale(2); }
.game:after { content: ''; position: absolute; left: 280px; top: 110px; width: 420px; height: 800px; background: linear-gradient(100deg,#283c49,#1a2a35); transform: skewY(-12deg); box-shadow: 930px 60px 0 150px #203440; opacity: .65; }
.preview-label { position: absolute; top: 32px; left: 36px; font-size: 13px; letter-spacing: 2px; color: #8ba5b6; }
.preview-note { position: absolute; bottom: 32px; width: 100%; text-align: center; color: #7893a4; font-size: 12px; }
#sw_root { display: flex; align-items: center; justify-content: center; }
.root, #sw_root { pointer-events: none; }
.menu-box { pointer-events: auto; }
.menu-box { overflow: hidden; }
.topbar, .heading, .section-line, .menu-footer, .item { align-items: center; }
.brand-mark, .item-symbol, .close-btn, .page-btn, .nav-btn { display: flex; align-items: center; justify-content: center; }
.operator { align-items: center; }
.item-body { overflow: hidden; }
.menu-title, .item-text, .item-comment, #sw_operator, #sw_context { overflow: hidden; }
.count-badge { flex-shrink: 0; }
.dashboard .item { align-items: flex-start; }
.dashboard .item-body { flex: 1; }
.dashboard .item-arrow { position: absolute; right: 26px; bottom: 26px; margin: 0; }
.item-image { object-fit: cover; }
.icon-players .players-icon, .icon-maps .maps-icon, .icon-warning .warning-icon { display: block !important; }
.symbol-icon { position: absolute; left: 50%; top: 50%; transform: translate(-50%, -50%); }
.footer-text { overflow: hidden; }
#toast { position: absolute; bottom: 92px; left: 50%; transform: translateX(-50%); padding: 14px 24px; border: 1px solid #70e0c455; border-radius: 8px; background: #132a27; color: #a7efdb; display: none; }
#reopen { position: absolute; inset: 0; margin: auto; width: 200px; height: 48px; border: 1px solid #70e0c455; border-radius: 8px; background: #132a27; text-align: center; display: none; }
'''
script = r'''
const $ = id => document.getElementById(id);
const text = (id, value) => { $(id).textContent = value; };
const maps = Object.entries({kz_angina:'3636272568',kz_angina_x:'3746453872',kz_antimony:'3609954118',kz_asteroid_beach:'3771237485',kz_avalon:'3583337319',kz_bayharborbhopper:'3593758954',kz_bhop_apricity:'3514400945',kz_bhop_majka:'3764093804',kz_bhop_slide:'3636997995'});
const people = ['XBDJ', 'Alice', '山海', 'KZ enjoyer', '夜航星', 'a_very_long_player_name_for_layout_check', '青空'];
const stack = [];
const item = (name, comment, action, glyph='', danger=false, disabled=false) => ({name, comment, action, glyph, danger, disabled});
function push(title, comment, items, layout='list') { stack.push({title,comment,items,layout,page:0}); render(); }
function toast(message) { text('toast', '预览：'+message); $('toast').style.display='block'; clearTimeout(window.toastTimer); window.toastTimer=setTimeout(()=>$('toast').style.display='none',3500); }
function home() { stack.length=0; push('管理员面板','管理当前服务器，让每一次操作清晰、可控。',[
 item('玩家操作','查看在线玩家，执行踢出、封禁与交流管理。',players,'P'),
 item('地图管理','浏览服务器地图列表，选择下一张目的地。',mapList,'M')], 'dashboard'); }
function players() { push('玩家操作','选择一位在线玩家，查看可用操作。受保护的玩家不可选择。',people.map((name,i)=>item(name+' ['+(i%2?'CT':'SPEC')+']',i===0?'目标受免疫等级保护。':'#'+i+'  ·  7656119800000000'+i,()=>actions(name),'P',false,i===0))); }
function actions(name) { push('操作: '+name,'当前目标 · '+name,[
 item('踢出','断开连接，玩家仍可重新加入',()=>confirm('踢出 '+name+'?', '即将踢出 '+name+'。玩家将立即断开连接，之后仍可重新加入。','确认踢出',()=>{toast('已踢出 '+name);home();},true),'!',true),
 item('封禁','选择时长，限制玩家加入服务器',()=>durations(name),'!',true),
 ...['处决','拍击（10 伤害）','重生','冻结（5 秒）','禁麦（默认时长）','禁言（默认时长）','沉默（默认时长）'].map(action=>item(action,'',()=>toast('已执行'+action+' · '+name))) ]); }
function durations(name) { push('封禁: '+name,'选择封禁时长，下一步核对并确认。',['30 秒','60 秒','1 小时','1 天','3 天','7 天','30 天','永久'].map(duration=>item(duration,'',()=>confirm('封禁 '+name+'?','即将封禁 '+name+'，时长：'+duration+'。确认后将断开连接并限制加入服务器。','确认封禁',()=>{toast('已封禁 '+name+' · '+duration);home();},true)))); }
function mapList() { push('地图管理','从配置的地图中选择。未安装的工坊地图会在确认后按需下载。',maps.map(([name,id])=>item(name,'Workshop ID: '+id,()=>confirm('更换地图至 '+name+'?','Workshop ID: '+id+' · 确认后开始换图倒计时，未安装的地图将按需下载。','确认更换地图',()=>{toast('开始换图 · '+name);home();}),'M'))); }
function confirm(title, comment, label, action, danger=false) { push(title,comment,[item(label,'',action,danger?'!':'M',danger),item('取消','返回上一步',()=>{stack.pop();render();})],'confirmation'); }
function render() {
 const frame=stack.at(-1), size=frame.layout==='dashboard'?2:5, pages=Math.ceil(frame.items.length/size), rows=frame.items.slice(frame.page*size,(frame.page+1)*size);
 $('sw_root').className=frame.layout;
 text('sw_brand','服务器管理'); text('sw_operator','XBDJ'); text('sw_context',stack.slice(-3).map(x=>x.title).join('  /  '));
 text('sw_title',frame.title); text('sw_comment_text',frame.comment); text('sw_count',frame.items.length+' 个选项'); text('sw_section',frame.layout==='confirmation'?'请核对操作信息':'可用操作');
 text('sw_page',(frame.page+1)+' / '+pages); text('sw_footer','点击选项继续操作'); text('sw_home_text','首页'); text('sw_back_text','返回');
 for(let i=0;i<5;i++) {
  const row=rows[i], el=$('sw_item'+i); el.className='item'+(!row?' hidden':'')+(row?.danger?' destructive':'')+(row?.disabled?' disabled':'')+({P:' icon-players',M:' icon-maps','!':' icon-warning'}[row?.glyph]||''); el.disabled=!row||!!row.disabled;
  if(!row) continue;
  text('sw_item'+i+'_glyph',row.glyph||String(frame.page*size+i+1).padStart(2,'0')); text('sw_item'+i+'_text',row.name); text('sw_item'+i+'_comment',row.comment); text('sw_item'+i+'_action',row.disabled?'不可用':frame.layout==='dashboard'?'进入管理':'选择'); el.onclick=row.action;
 }
 for(const id of ['sw_home','sw_back']) $(id).classList.toggle('hidden',stack.length<2);
 $('sw_prev').disabled=frame.page===0; $('sw_next').disabled=frame.page===pages-1;
 $('sw_prev').classList.toggle('disabled',$('sw_prev').disabled); $('sw_next').classList.toggle('disabled',$('sw_next').disabled);
}
$('sw_prev').onclick=()=>{stack.at(-1).page--;render();}; $('sw_next').onclick=()=>{stack.at(-1).page++;render();};
$('sw_back').onclick=()=>{stack.pop();render();}; $('sw_home').onclick=home;
$('sw_close').onclick=()=>{$('sw_root').classList.add('hidden');$('reopen').style.display='block';};
$('reopen').onclick=()=>{$('reopen').style.display='none';home();};
function resize(){const scale=Math.min(innerWidth/1920,innerHeight/1080);$('stage').style.transform='scale('+scale+')';$('stage').style.left=(innerWidth-1920*scale)/2+'px';$('stage').style.top=(innerHeight-1080*scale)/2+'px';}
window.addEventListener('resize',resize);home();resize();
'''
output = '<!doctype html><html lang="zh-CN"><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>SWS2 管理员 HUD · 交互预览</title><style>'+css+'\n'+adapter+'</style><div id="stage"><div class="game"></div><div class="preview-label">SWS2 / CUSTOM HUD</div>'+body+'<div class="preview-note">界面交互预览 · 示例数据 · 游戏内使用 !admin 打开</div><div id="toast" role="status"></div><button id="reopen">重新打开 !admin</button></div><script>'+script+'</script></html>'
(ROOT/'preview').mkdir(exist_ok=True)
(ROOT/'preview/index.html').write_text(output)
print('Generated preview/index.html from Panorama XML and CSS')
