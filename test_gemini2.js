const fs = require('fs');

const prompt = `You are an expert travel assistant. Based on the following user preferences, recommend the top 3 best matching destinations anywhere in the world (or specifically matching their criteria):
- Budget: 5000000 VND
- Days: 3
- Interests: culture
- Departure from: TP. Hồ Chí Minh
- Destination to: 
- Transportation: airplane
- Travel Group: friends
- Destination Type: city
- Main Goal: relaxation
- Preferred Weather: cool
- Accommodation: hotel
- Budget Style: balanced

For each destination, provide the Name, CityProvince (or Country), a MatchReason explaining why it's a good fit based on ALL their specific preferences, and an EstimatedCostVND (total estimated cost for 3 days).
Also provide a \`distance\` field which is the estimated distance (e.g. "1200 km") from TP. Hồ Chí Minh to the recommended destination.
Also provide a \`dailyCostBreakdown\` object containing estimated daily cost ranges in VND (as strings, e.g., "300.000đ - 500.000đ") for the following categories: accommodation, food, transportation, activities, entertainment, shopping.

Return the response exactly as a JSON array matching this structure, without any markdown formatting or extra text:
[
  {
    "name": "Destination Name",
    "cityProvince": "City/Country Name",
    "matchReason": "Detailed reason here...",
    "distance": "1200 km",
    "estimatedCostVND": 1000000,
    "dailyCostBreakdown": {
      "accommodation": "300.000đ - 500.000đ",
      "food": "200.000đ - 400.000đ",
      "transportation": "100.000đ - 200.000đ",
      "activities": "150.000đ - 300.000đ",
      "entertainment": "100.000đ - 200.000đ",
      "shopping": "100.000đ - 300.000đ"
    }
  }
]`;

const data = {
  contents: [{ parts: [{ text: prompt }] }]
};

fetch('https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=YOUR_GEMINI_API_KEY', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(data)
})
.then(res => res.json())
.then(json => {
  const text = json.candidates[0].content.parts[0].text;
  fs.writeFileSync('gemini_output.txt', text);
  console.log('Done writing gemini_output.txt');
})
.catch(err => console.error('Error:', err.message));
