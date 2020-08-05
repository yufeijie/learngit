function plotMulti(headline, xLabelName, yLabelName, total, number, id, size, xData, yData)
% plotPareto �������ͼ
% headline �����
% xLabelName yLabelName ��������
% total ����ͼ��
% number ������ͼ��������
% id ��ͼ��ӦID
% size ������������
% data ��������

figure(10)

total = double(total);
for i = 1:total
    subplot(total, 1, i);
    for j = 1:number(i)
        k = id(i, j);
        plot(xData(k, 1:size(k)), yData(k, 1:size(k)));
        hold on
    end
    xlabel(xLabelName)
    ylabel(yLabelName)
end

suptitle(headline)
end