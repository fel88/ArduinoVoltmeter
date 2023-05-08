#include <Adafruit_GFX.h>      //Libraries for the OLED and BMP280
#include <Adafruit_SSD1306.h>

#define SCREEN_WIDTH 128 // OLED display width, in pixels
#define SCREEN_HEIGHT 32 // OLED display height, in pixels
#define OLED_RESET    -1 // Reset pin # (or -1 if sharing Arduino reset pin)

#define NUM_READINGS 20
float voltage=0;

float readings[NUM_READINGS];
int reading_idx=0;
float R1=68;
float R2=68;
int _delay=200;

Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET); //Declaring the display name (display)

float lastVoltage=0;
float decay=0;

unsigned long last = 0;
 
void setup() {  
  
  Serial.begin(115200); 
  Serial.println("Arduino Voltmeter started.."); 
  
  display.begin(SSD1306_SWITCHCAPVCC, 0x3C); //Start the OLED display
  display.clearDisplay();
  //display.setRotation(2);
  display.display();
  
  display.setTextColor(WHITE);
  display.setTextSize(1); 
  
  display.setCursor(32,12);
  display.setTextSize(2);          
  
  last=millis();
  //analogReference(INTERNAL);
}


 
void loop() {
  int volt= analogRead(A0);
  float sum=0;
  readings[reading_idx++]=volt * (5.0/1024) * ((R1 + R2)/R2);;

  if(reading_idx>=NUM_READINGS)
    reading_idx=0;

  for(int i=0;i<NUM_READINGS;i++){
      sum+=readings[i];  
  }

  float res=sum/NUM_READINGS;
  //voltage=0.9*voltage+0.1*res;
  voltage=res;

  decay=0.95*decay+0.05*(lastVoltage-voltage);
  //decay=(lastVoltage-voltage);
  lastVoltage=voltage;

  //voltage =volt/1023.0*5.0;
  Serial.print("Voltage = ");
  Serial.print(voltage,4);
  Serial.println("V");
  // delay(1000);
  //Serial.println("cp1");
  display.clearDisplay();
    
  display.setCursor(0,0);                   //Oled display, just playing with text size and cursor to get the display you want
  display.setTextColor(WHITE);
  display.setTextSize(2); 
  display.print("V");
    
  display.setCursor(50,18-15);
  display.print(voltage,3);
  display.setCursor(50,17);

  int hours=0;
  int minutes=0;
  int secs=0;
  if(decay<0.00001){
      decay=0.00001;
    }
    
   unsigned long now = millis();
     

  Serial.println("decay");
  Serial.println(decay);
  Serial.println((voltage/decay));
  
  float diff=now-last;
  Serial.println("koef");
  Serial.println(1000.0/diff);
     
  int totalSecsLast=(voltage/decay)*(1000.0/diff);
  Serial.println("totalSecsLast");
  Serial.println(totalSecsLast);
  if(totalSecsLast<0)
    totalSecsLast=0;
     
  last=now;
  secs=totalSecsLast%60;
  hours=totalSecsLast/(60*60);
  minutes=(totalSecsLast/60)%60;
    
  display.setTextSize(1);

  display.setCursor(0,25);
  display.print("dec");
  display.setCursor(30,25);
  display.print(decay,4);
    
  display.setCursor(80,25);
  //display.print(hours);
  display.print(totalSecsLast);
  display.setCursor(85,25);
  //display.print(minutes);

  display.setCursor(100,25);
  //display.print(secs);
    
    
  display.display();
  delay(_delay);    
}
