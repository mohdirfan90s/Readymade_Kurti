// Global Language Switching Script for Garment Lot Manager

function getAppLanguage() {
    return localStorage.getItem('garment_app_lang') || 'hi';
}

function switchLanguage(lang) {
    if (lang !== 'hi' && lang !== 'en') lang = 'hi';
    localStorage.setItem('garment_app_lang', lang);
    document.documentElement.setAttribute('data-lang', lang);
    document.documentElement.setAttribute('lang', lang);

    // Update active button state
    const btnHi = document.getElementById('btnLangHi');
    const btnEn = document.getElementById('btnLangEn');
    const btnHiMob = document.getElementById('btnLangHiMob');
    const btnEnMob = document.getElementById('btnLangEnMob');

    if (btnHi) btnHi.classList.toggle('active', lang === 'hi');
    if (btnEn) btnEn.classList.toggle('active', lang === 'en');
    if (btnHiMob) btnHiMob.classList.toggle('active', lang === 'hi');
    if (btnEnMob) btnEnMob.classList.toggle('active', lang === 'en');

    // Update form placeholders
    document.querySelectorAll('[data-hi-placeholder]').forEach(el => {
        const ph = lang === 'en' ? el.getAttribute('data-en-placeholder') : el.getAttribute('data-hi-placeholder');
        if (ph) el.setAttribute('placeholder', ph);
    });

    // Fire custom event
    window.dispatchEvent(new CustomEvent('languageChanged', { detail: { language: lang } }));
}

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', function() {
    const current = getAppLanguage();
    switchLanguage(current);
});

// Global WhatsApp Lot Share
function shareLotWhatsApp(lotNo, date, merchant, item, color, pcs, rate, revenue, expense, profit, status) {
    let msg = `*🧵 ગારમેન્ટ લોટ હિસાબ*\n`;
    msg += `📦 *લોટ:* ${lotNo}\n`;
    if (date) msg += `📅 *તારીખ:* ${date}\n`;
    msg += `👤 *વેપારી:* ${merchant}\n`;
    msg += `👕 *માલ:* ${item}${color ? ` (${color})` : ''}\n`;
    msg += `✂️ *પીસ:* ${pcs} pcs @@ ₹${rate}/pc\n`;
    msg += `💰 *વેપારી રકમ:* ₹${revenue}\n`;
    msg += `📦 *કુલ ખર્ચ:* ₹${expense}\n`;
    msg += `📈 *મુનાફા:* ₹${profit}\n`;
    msg += `✅ *સ્ટેટસ:* ${status}\n\n`;
    msg += `_ગારમેન્ટ લોટ મેનેજર_`;

    const url = `https://api.whatsapp.com/send?text=${encodeURIComponent(msg)}`;
    window.open(url, '_blank');
}
