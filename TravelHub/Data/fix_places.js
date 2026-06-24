const fs = require('fs');

const raw = fs.readFileSync('c:/Users/ADMIN/source/repos/TravelHub/TravelHub/Data/places.json', 'utf8');
const places = JSON.parse(raw);

function fixText(str) {
    if (!str || typeof str !== 'string') return str;
    
    // The corruption mapped 'Tr' to '00000', 'tr' to '00000', 'K' to '000', 'k' to '000'
    // Let's do a smart replace.
    // If 00000 is at the start of the string or after a space, capitalize it (Tr)
    let res = str.replace(/(^|\s)00000/g, '$1Tr')
                 .replace(/00000/g, 'tr')
                 .replace(/(^|\s)000/g, '$1K')
                 .replace(/000/g, 'k');
                 
    // Fix some specific broken diacritics
    res = res.replace(/000h\u00E1ch/g, 'khách'); // khách
    res = res.replace(/000h\u00F4ng/g, 'không'); // không
    res = res.replace(/00000ong/g, 'trong'); // trong
    res = res.replace(/00000\u00ECnh/g, 'trình'); // trình
    res = res.replace(/000i\u1EBFn 00000\u00FAc/g, 'kiến trúc'); // kiến trúc

    return res;
}

places.forEach(p => {
    if (p.Name) p.Name = fixText(p.Name);
    if (p.CityProvince) p.CityProvince = fixText(p.CityProvince);
    if (p.Description) p.Description = fixText(p.Description);
    if (p.KeyMain) p.KeyMain = fixText(p.KeyMain);
});

fs.writeFileSync('c:/Users/ADMIN/source/repos/TravelHub/TravelHub/Data/places.json', JSON.stringify(places, null, 2), 'utf8');
console.log("Fixed places.json");
