const data = { 
  contents: [{ 
    parts: [{ 
      text: 'Please output exactly this JSON array: [{"name":"A","cityProvince":"B","matchReason":"C","distance":"D","estimatedCostVND":1000,"dailyCostBreakdown":{"accommodation":"E","food":"F","transportation":"G","activities":"H","entertainment":"I","shopping":"J"}}]' 
    }] 
  }] 
}; 
fetch('https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=YOUR_GEMINI_API_KEY', { 
  method: 'POST', 
  headers: { 'Content-Type': 'application/json' }, 
  body: JSON.stringify(data) 
})
.then(res => res.json())
.then(json => console.log('Result:', JSON.stringify(json)))
.catch(err => console.error('Error:', err.message));
